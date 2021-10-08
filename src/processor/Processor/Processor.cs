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
        var results = input.Blobs.ToDictionary(x => x.BlobName, x => new ProcessBlobResult() { Blob = x });

        do
        {
            for(int i=0; i<options.MaxRetries; i++)
            {
                var unprocessed = postMode ? results.Values.Where(x => String.IsNullOrEmpty(x.OperationId)).ToArray() :
                                             results.Values.Where(x => x.Blob.State == BlobInfo.ProcessState.Unprocessed).ToArray();

                if (!unprocessed.Any()) break;

                foreach (var blob in unprocessed)
                {
                    string operationId = null;
                    if (results.ContainsKey(blob.Blob.BlobName))
                        operationId = blob.OperationId;
                    var result = await ProcessBlob(blob.Blob, operationId, postMode, log);
                    results[result.Blob.BlobName] = result;
                }
            }

            postMode = !postMode;
        }
        while (!postMode);
 
        await cosmosService.SaveDocuments(results.Values.Select(x => new Document() { 
                Id = x.Blob.BlobName,
                State = x.Blob.State.ToString(),
                Forms = x.Forms?.Select(x => SerializeForm(x)),
                Exception = x.Exception,
                TransientFailureCount = x.Blob.TransientFailureCount
            }));

        return results.Values.Select(x => x.Blob);
    }

    [FunctionName("Processor_UpdateState")]
    public async Task UpdateState([ActivityTrigger] IEnumerable<BlobInfo> blobs, ILogger log)
    {
        await blobStorageService.UpdateState(blobs);
    }

    private async Task<ProcessBlobResult> ProcessBlob(BlobInfo blob, string operationId, bool postMode, ILogger log)
    {
        try
        {
            var result = new ProcessBlobResult() { Blob = blob, OperationId = operationId };

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
                using(var stream = await blobStorageService.DownloadStream(blob.BlobName))
                {
                    result.OperationId = await formRecognizerService.SubmitDocument(options.FormRecognizerModelId, stream);
                    if (String.IsNullOrEmpty(result.OperationId)) blob.TransientFailureCount++;
                }
            }                
            else
            {   
                if (!String.IsNullOrEmpty(result.OperationId))
                {                    
                    var formRecognizerResult = await formRecognizerService.RetreiveResults(result.OperationId);                    
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.TransientFailure) blob.TransientFailureCount++;
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.CompletedWithoutResult) blob.State = BlobInfo.ProcessState.Processed;
                    if (formRecognizerResult.Status == FormRecognizerResult.ResultStatus.CompletedWithResult) 
                    {
                        result.Forms = formRecognizerResult.Forms;
                        blob.State = BlobInfo.ProcessState.Processed;
                    }
                }
            }
            return result;
        }
        catch (Exception e)
        {
            log.LogError(e.ToString());
            blob.State = BlobInfo.ProcessState.Failed;
            return new ProcessBlobResult() { Blob = blob, Exception = e.ToString() };
        }
    }

    private JObject SerializeForm(RecognizedForm form)
    {
        return new JObject(new JProperty("FormType", form.FormType),
                           new JProperty("FormTypeConfidence", form.FormTypeConfidence),
                           new JProperty("ModelId", form.ModelId),
                           new JProperty("Fields",
                                 new JArray(form.Fields.Select(x => 
                                    new JObject(new JProperty("Name", x.Value.Name),
                                                new JProperty("Confidence", x.Value.Confidence),
                                                new JProperty("Value", x.Value.Value.AsString()))))));
    }
}