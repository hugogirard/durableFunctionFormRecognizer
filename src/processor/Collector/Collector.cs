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
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var continuationToken = context.GetInput<string>();

        var count = await context.CallActivityAsync<int>("Collector_Count", options.NbPartitions);

        if (count < options.MinBacklogSize)
        {
            var output = await context.CallActivityAsync<CollectorOutput>("Collector_Collect", continuationToken);
            continuationToken = output.ContinuationToken;

            int i = 0;
            foreach (var blobInfoEntity in output.Blobs.Partition(options.PartitionSize))
            {
                var entityId = BlobInfoEntity.GetEntityId(i);
                context.SignalEntity(entityId, "AddIfNew", blobInfoEntity);
                i++;
            }
        }

        await context.CreateTimer(context.CurrentUtcDateTime.Add(options.CollectDelay), CancellationToken.None);
        
        context.ContinueAsNew(continuationToken);
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
        log.LogInformation("Collecting blobs...");
        return await blobStorageService.GetUnprocessedBlobs(options.BatchSize, continuationToken);
    }
}