using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<ProcessorInput>();

        input.Stats.LatestRunStartTime = context.CurrentUtcDateTime;

        var entityId = BlobInfoEntity.GetEntityId(input.PartitionId);
        var blobs = await context.CallEntityAsync<IEnumerable<BlobInfo>>(entityId, "Reserve", options.PartitionSize);

        if (blobs.Any())
        {
            entityId = BlobInfoEntity.GetEntityId(input.PartitionId);
            blobs = await context.CallActivityAsync<IEnumerable<BlobInfo>>("Processor_ProcessPartition", 
                new ProcessInput() { PartitionId = input.PartitionId, Blobs = blobs });
            
            await context.CallActivityAsync("Processor_UpdateState", blobs);

            input.Stats.TotalProcessed += blobs.Count(x => x.State == BlobInfo.ProcessState.Processed);
            input.Stats.TotalFailed += blobs.Count(x => x.State == BlobInfo.ProcessState.Failed);
            input.Stats.TotalTransientFailures += blobs.Sum(x => x.TransientFailureCount);                
        }
        else
        {
            await context.CreateTimer(context.CurrentUtcDateTime.Add(options.NoDataDelay), CancellationToken.None);
        }

        context.ContinueAsNew(input);
    }

    [FunctionName("Processor_ProcessPartition")]
    public async Task<IEnumerable<BlobInfo>> ProcessPartition([ActivityTrigger] ProcessInput input, ILogger log)
    {
        var postMode = true;
        var processBlobInfos = input.Blobs.ToDictionary(x => x.BlobName, x => new ProcessBlobInfo() { Blob = x });

        do
        {
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
 
        await cosmosService.SaveDocuments(processBlobInfos.Values.Select(x => new Document() { 
                Id = x.Blob.BlobName,
                State = x.Blob.State.ToString(),
                Forms = x.Forms?.Select(x => new Document.Form(x)),
                Exception = x.Exception,
                TransientFailureCount = x.Blob.TransientFailureCount
            }));

        return processBlobInfos.Values.Select(x => x.Blob);
    }

    [FunctionName("Processor_UpdateState")]
    public async Task UpdateState([ActivityTrigger] IEnumerable<BlobInfo> blobs, ILogger log)
    {
        await blobStorageService.UpdateState(blobs);
    }

    private async Task<ProcessBlobInfo> ProcessBlob(ProcessBlobInfo processBlobInfo, bool postMode, ILogger log)
    {
        try
        {
            // // Simulate failures and latency
            // var rnd = new Random();
            // if (rnd.Next(1, 10) == 1) throw new ApplicationException("Random failure");
            // if (rnd.Next(1, 10) == 2) { Thread.Sleep(2000); log.LogInformation("Random delay"); }
            // if (rnd.Next(1, 10) == 3)
            // {
            //     log.LogInformation("Transient failure");
            //     blob.TransientFailureCount++;
            //     return result;
            // }

            if (postMode) 
            {
                using(var stream = await blobStorageService.DownloadStream(processBlobInfo.Blob.BlobName))
                {
                    processBlobInfo.OperationId = await formRecognizerService.SubmitDocument(options.FormRecognizerModelId, stream);
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

                    var formRecognizerResult = await formRecognizerService.RetreiveResults(processBlobInfo.OperationId);                    
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
}