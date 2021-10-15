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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

public class Processor
{
    private ProcessorOptions options;
    private IBlobStorageService blobStorageService;
    private IFormRecognizerService formRecognizerService;
    private ICosmosService cosmosService;

    public Processor(ProcessorOptions options, 
                     IBlobStorageService blobStorageService, 
                     IFormRecognizerService formRecognizerService, 
                     ICosmosService cosmosService)
    {
        this.options = options;
        this.blobStorageService = blobStorageService;
        this.formRecognizerService = formRecognizerService;
        this.cosmosService = cosmosService;
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
            var entityId = BlobInfoEntity.GetEntityId(input.PartitionId);
            log.LogInformation($"{prefix} Reserving blobs...");
            var blobs = await context.CallEntityAsync<IEnumerable<BlobInfo>>(entityId, "Reserve", options.PartitionSize);

            if (blobs.Any())
            {
                log.LogInformation($"{prefix} Processing blobs...");
                blobs = await context.CallActivityAsync<IEnumerable<BlobInfo>>("Processor_ProcessPartition", 
                    new ProcessInput() { PartitionId = input.PartitionId, Blobs = blobs });
                
                log.LogInformation($"{prefix} Updating blob states...");
                await context.CallActivityAsync("Processor_UpdateState", blobs);

                // Note: This can create a race condition where the Collecter adds back blobs 
                // already processed but the risk is low
                log.LogInformation($"{prefix} Clearing blob reservation...");
                await context.CallEntityAsync(entityId, "ClearReserved");

                input.Stats.TotalProcessed += blobs.Count(x => x.State == BlobInfo.ProcessState.Processed);
                input.Stats.TotalFailed += blobs.Count(x => x.State == BlobInfo.ProcessState.Failed);
                input.Stats.TotalTransientFailures += blobs.Sum(x => x.TransientFailureCount);                
            }
            else
            {
                log.LogInformation($"{prefix} No blobs to process, going to sleep...");
                await context.CreateTimer(context.CurrentUtcDateTime.Add(options.NoDataDelay), CancellationToken.None);
            }

            log.LogInformation($"{prefix} Starting new instance ...");
            context.ContinueAsNew(input);
        }
        catch(Exception ex)
        {
            log.LogError($"{prefix} Processor failed with the following exception: {ex.ToString()}");
            input.PreviousInstanceId = context.InstanceId;
            await context.CallActivityAsync("Processor_Restart", input);
            throw;
        }
    }

    [FunctionName("Processor_ProcessPartition")]
    public async Task<IEnumerable<BlobInfo>> ProcessPartition([ActivityTrigger] ProcessInput input, ILogger log)
    {
        var postMode = true;
        var prefix = $"[Processor:{input.PartitionId}]";
        var processBlobInfos = input.Blobs.ToDictionary(x => x.BlobName, x => new ProcessBlobInfo() { Blob = x });

        do
        {
            if(postMode) log.LogInformation($"{prefix} Submitting documents to Form Recognizer...");
            else log.LogInformation($"{prefix} Getting Form Recognizer results...");

            for(int i=0; i<options.MaxRetries; i++)
            {
                var unprocessedBlobInfos = postMode ? processBlobInfos.Values.Where(x => String.IsNullOrEmpty(x.OperationId)).ToArray() :
                                                      processBlobInfos.Values.Where(x => x.Blob.State == BlobInfo.ProcessState.Unprocessed).ToArray();

                if (!unprocessedBlobInfos.Any()) break;

                foreach (var processBlobInfo in unprocessedBlobInfos)
                {
                    processBlobInfos[processBlobInfo.Blob.BlobName] = 
                        await ProcessBlob(processBlobInfos[processBlobInfo.Blob.BlobName], postMode, log);
                }
            }

            postMode = !postMode;
        }
        while (!postMode);
 
        log.LogInformation($"{prefix} Saving documentsa to Cosmos...");
        await cosmosService.SaveDocuments(processBlobInfos.Values.Select(x => new Document() { 
                Id = x.Blob.BlobName,
                State = x.Blob.State.ToString(),
                Forms = x.Forms?.Select(x => new Document.Form(x)),
                Exception = x.Exception,
                TransientFailureCount = x.Blob.TransientFailureCount
            }));

        return processBlobInfos.Values.Select(x => x.Blob);
    }

    private async Task<ProcessBlobInfo> ProcessBlob(ProcessBlobInfo processBlobInfo, bool postMode, ILogger log)
    {
        try
        {
            if (postMode) 
            {
                using(var stream = await blobStorageService.DownloadStream(processBlobInfo.Blob.BlobName))
                {
                    processBlobInfo.OperationId = await formRecognizerService.SubmitDocument(options.FormRecognizerModelId, stream, log);
                    processBlobInfo.StartTime = DateTime.Now;
                    if (String.IsNullOrEmpty(processBlobInfo.OperationId)) processBlobInfo.Blob.TransientFailureCount++;
                }
            }                
            else
            {   
                if (!String.IsNullOrEmpty(processBlobInfo.OperationId))
                {
                    var timeToSleep = processBlobInfo.StartTime + options.MinProcessingTime - DateTime.Now;
                    if (timeToSleep > TimeSpan.Zero) Thread.Sleep(timeToSleep);

                    var formRecognizerResult = await formRecognizerService.RetreiveResults(processBlobInfo.OperationId, log);                    
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.TransientFailure) processBlobInfo.Blob.TransientFailureCount++;
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.CompletedWithoutResult) processBlobInfo.Blob.State = BlobInfo.ProcessState.Processed;
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.CompletedWithResult) 
                    {
                        processBlobInfo.Forms = formRecognizerResult.Forms;
                        processBlobInfo.Blob.State = BlobInfo.ProcessState.Processed;
                    }
                }
            }
            return processBlobInfo;
        }
        catch (Exception e)
        {
            log.LogError(e.ToString());
            processBlobInfo.Exception = e.ToString();
            processBlobInfo.Blob.State = BlobInfo.ProcessState.Failed;
            return processBlobInfo;
        }
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