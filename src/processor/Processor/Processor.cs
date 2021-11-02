/*
* Notice: Any links, references, or attachments that contain sample scripts, code, or commands comes with the following notification.
*
* This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
* THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED,
* INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
*
* We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code,
* provided that You agree:
*
* (i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded;
* (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and
* (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits,
* including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.
*
* Please note: None of the conditions outlined in the disclaimer above will superseded the terms and conditions contained within the Premier Customer Services Description.
*
* DEMO POC - "AS IS"
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Polly;

public class Processor
{
    private ProcessorOptions options;
    private IBlobStorageService blobStorageService;
    private IFormRecognizerService formRecognizerService;
    private IDocumentService documentService;

    public Processor(ProcessorOptions options, 
                     IBlobStorageService blobStorageService, 
                     IFormRecognizerService formRecognizerService, 
                     IDocumentService documentService)
    {
        this.options = options;
        this.blobStorageService = blobStorageService;
        this.formRecognizerService = formRecognizerService;
        this.documentService = documentService;
    }

   [FunctionName("Processor")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
    {
        log = context.CreateReplaySafeLogger(log);
        var input = context.GetInput<ProcessorInput>();
        var prefix = $"[Processor:{input.PartitionId}]";

        try
        {
            log.LogInformation($"{prefix} Reserving blobs...");
            var blobs = await context.CallEntityAsync<IEnumerable<BlobInfo>>(
                BlobInfoEntity.EntityId, "Reserve", (input.PartitionId, options.PartitionSize));

            if (blobs.Any())
            {
                log.LogInformation($"{prefix} Processing {blobs.Count()} blobs...");
                blobs = await context.CallActivityAsync<IEnumerable<BlobInfo>>("Processor_ProcessPartition", 
                    new PartitionInfo() { PartitionId = input.PartitionId, Blobs = blobs });
                
                log.LogInformation($"{prefix} Updating blob tags/states...");
                await context.CallActivityAsync("Processor_UpdateState", blobs);

                log.LogInformation($"{prefix} Clearing blob reservation...");
                await context.CallEntityAsync(BlobInfoEntity.EntityId, "ClearReserved", (input.PartitionId, blobs));

                // This keeps track of the overall statistics of the processor
                input.Stats.TotalProcessed += blobs.Count(x => x.State == BlobInfo.ProcessState.Processed);
                input.Stats.TotalFailed += blobs.Count(x => x.State == BlobInfo.ProcessState.Failed);
                input.Stats.TotalTransientFailures += blobs.Sum(x => x.TransientFailureCount);                
            }
            else
            {
                log.LogInformation($"{prefix} No blobs to process, going to sleep for {options.NoDataDelay.TotalSeconds} seconds...");
                await context.CreateTimer(context.CurrentUtcDateTime.Add(options.NoDataDelay), CancellationToken.None);
            }

            log.LogInformation($"{prefix} Starting new instance ...");
            context.ContinueAsNew(input);
        }
        catch(Exception ex)
        {
            // In case of a failure, the current instance will fail and an new one is started with a reference to the current
            log.LogError($"{prefix} Processor failed with the following exception: {ex.ToString()}");
            input.PreviousInstanceId = context.InstanceId;
            await context.CallActivityAsync("Processor_Restart", input);
            throw;
        }
    }

    [FunctionName("Processor_ProcessPartition")]
    public async Task<IEnumerable<BlobInfo>> ProcessPartition([ActivityTrigger] PartitionInfo input, ILogger log)
    {
        var postMode = true;
        var prefix = $"[Processor:{input.PartitionId}]";
        var processInfos = input.Blobs.ToDictionary(x => x.BlobName, x => new ProcessInfo() { Blob = x });

        // The retries are controlled by a Polly policy with exponential backoff
        // Only transient failures (throttling) and incomplete operations are retried
        var policy = Policy
            .Handle<TransientFailureException>()
            .Or<IncompleteOperationException>()
            .WaitAndRetryAsync(options.MaxRetries, retryAttempt => TimeSpan.FromMilliseconds(
                Math.Pow(options.RetryMillisecondsPower, retryAttempt) * options.RetryMillisecondsFactor)
            );        

        do
        {            
            if(postMode) log.LogInformation($"{prefix} Submitting documents to Form Recognizer...");
            else log.LogInformation($"{prefix} Getting Form Recognizer results...");

            // This loop runs twice, once is post mode to submit documents to Form Recognizer and
            // once to get results form Form Recognizer
            foreach (var processInfo in processInfos.Values.ToArray())
            {
                try
                {
                    await policy.ExecuteAsync(async () => 
                    {
                        var watch = Stopwatch.StartNew();
                        try
                        {
                            processInfos[processInfo.Blob.BlobName] = 
                                await ProcessBlob(processInfos[processInfo.Blob.BlobName], postMode, log);
                        }
                        // The two following exceptions are retrown to be handled by Polly
                        catch (TransientFailureException)
                        {
                            processInfo.Blob.TransientFailureCount++;
                            throw;
                        }
                        catch (IncompleteOperationException) { throw; }
                        // Other exception types are irrecuperable and the blob will not be processed again
                        catch (Exception ex)
                        {
                            log.LogError($"{prefix} {ex.ToString()}");
                            processInfo.Exception = ex.ToString();
                            processInfo.Blob.State = BlobInfo.ProcessState.Failed;                            
                        }
                        finally
                        {
                            // This sleep is to controll the number of TPS sent to Form Recognizer
                            // It adjusts dynamically based on the time spent (ex: posts are longer than gets)
                            var delay = TimeSpan.FromMilliseconds(Math.Max(
                                0, (options.LoopDelay-watch.Elapsed).TotalMilliseconds));
                            Thread.Sleep(delay);
                        }
                    });
                }
                catch (Exception ex)
                {
                    // The max number of retries was reached by Polly, the blob will be re-introduced by the collector later
                    if (ex is IncompleteOperationException || ex is TransientFailureException)
                    {
                        log.LogWarning($"{prefix} Maximum number of retries reached for blob {processInfo.Blob.BlobName}, will stay unprocessed...");
                    }
                    else 
                    {
                        throw;
                    }
                }  
            }

            postMode = !postMode;
        }
        while (!postMode);
 
        log.LogInformation($"{prefix} Saving documents...");
        await documentService.SaveDocuments(processInfos.Values.
            Where(x => x.Blob.State != BlobInfo.ProcessState.Unprocessed).
                Select(x => new Document() { 
                    Id = x.Blob.BlobName,
                    State = x.Blob.State.ToString(),
                    Forms = x.Forms?.Select(x => new Document.Form(x)),
                    Exception = x.Exception,
                    TransientFailureCount = x.Blob.TransientFailureCount
                }));

        return processInfos.Values.Select(x => x.Blob);
    }

    private async Task<ProcessInfo> ProcessBlob(ProcessInfo processInfo, bool postMode, ILogger log)
    {
        if (postMode) 
        {
            using(var stream = await blobStorageService.DownloadStream(processInfo.Blob.BlobName))
            {
                processInfo.OperationId = await formRecognizerService.SubmitDocument(options.FormRecognizerModelId, stream, log);
                processInfo.StartTime = DateTime.Now;
            }
        }                
        else
        {   
            if (!String.IsNullOrEmpty(processInfo.OperationId))
            {
                // If amount left process is low, Form Recognizer might not have enough time to process, so it sleeps
                var timeToSleep = processInfo.StartTime + options.FormRecognizerMinWaitTime - DateTime.Now;
                if (timeToSleep > TimeSpan.Zero) Thread.Sleep(timeToSleep);

                processInfo.Forms = await formRecognizerService.RetreiveResults(processInfo.OperationId, log);
                processInfo.Blob.State = BlobInfo.ProcessState.Processed;
            }
        }
        return processInfo;    
    }

    [FunctionName("Processor_UpdateState")]
    public async Task UpdateState([ActivityTrigger] IEnumerable<BlobInfo> blobs, ILogger log)
    {
        await blobStorageService.UpdateState(blobs);
    }

    [FunctionName("Processor_Restart")]
    public async Task Restart([ActivityTrigger] ProcessorInput input, [DurableClient] IDurableOrchestrationClient client, ILogger log)
    {
        var prefix = $"[Processor:{input.PartitionId}]";
        log.LogWarning($"{prefix} Restarting processor...");
        await client.StartNewAsync<ProcessorInput>("Processor", input);
    }
}