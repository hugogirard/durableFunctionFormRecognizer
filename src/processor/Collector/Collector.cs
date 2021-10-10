using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

public class Collector
{
    private CollectorOptions options;
    private IBlobStorageService blobStorageService;

    public Collector(CollectorOptions options, IBlobStorageService blobStorageService)
    {
        this.options = options;
        this.blobStorageService = blobStorageService;
    }

    [FunctionName("Collector")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
    {
        log = context.CreateReplaySafeLogger(log);
        var input = context.GetInput<CollectorInput>() ?? new CollectorInput();

        try
        {
            log.LogInformation($"[Collector] Counting blobs...");
            var count = await context.CallActivityAsync<int>("Collector_Count", options.NbPartitions);
            log.LogInformation($"[Collector] Current blob count is {count}");

            if (count < options.MinBacklogSize)
            {
                log.LogInformation($"[Collector] Collecting more blobs...");
                var output = await context.CallActivityAsync<CollectorOutput>("Collector_Collect", input.ContinuationToken);
                input.ContinuationToken = output.ContinuationToken;

                int i = 0;
                log.LogInformation($"[Collector] Splitting blobs into partitions...");
                foreach (var blobInfoEntity in output.Blobs.Partition(options.PartitionSize))
                {
                    var entityId = BlobInfoEntity.GetEntityId(i);
                    context.SignalEntity(entityId, "AddIfNew", blobInfoEntity);
                    i++;
                }
            }

            log.LogInformation($"[Collector] Going to sleep...");
            await context.CreateTimer(context.CurrentUtcDateTime.Add(options.CollectDelay), CancellationToken.None);
            
            context.ContinueAsNew(input);
        }
        catch (Exception ex)
        {
            log.LogError($"[Collector] Collector failed with the following exception: {ex.ToString()}");
            input.PreviousInstanceId = context.InstanceId;
            await context.CallActivityAsync("Collector_Restart", input);
            throw;
        }
    }

    [FunctionName("Collector_Count")]
    public async Task<int> GetCount([ActivityTrigger] int nbPartitions, [DurableClient] IDurableEntityClient client, ILogger log)
    {
        var count = 0;
        for (int i = 0; i < nbPartitions; i++)
        {
            var entityId = BlobInfoEntity.GetEntityId(i);
            var state = await client.ReadEntityStateAsync<BlobInfoEntity>(entityId);
            if (state.EntityExists)
            {
                count += state.EntityState.Count();
            }
        }
        return count;
    }

    [FunctionName("Collector_Collect")]
    public async Task<CollectorOutput> Collect([ActivityTrigger] string continuationToken, ILogger log)
    {
        return await blobStorageService.GetUnprocessedBlobs(options.BatchSize, continuationToken);
    }

    [FunctionName("Collector_Restart")]
    public async Task Restart([ActivityTrigger] CollectorInput input, [DurableClient] IDurableOrchestrationClient client, ILogger log)
    {
        log.LogWarning($"[Collector] Restarting collector...");
        await client.StartNewAsync<CollectorInput>("Collector", input);
    }
}