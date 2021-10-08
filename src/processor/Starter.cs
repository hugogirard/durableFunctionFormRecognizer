using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

public class Starter
{
    private ProcessorOptions processorOptions;
    private IBlobStorageService blobStorageService;

    public Starter(ProcessorOptions processorOptions, IBlobStorageService blobStorageService)
    {
        this.processorOptions = processorOptions;
        this.blobStorageService = blobStorageService;
    }

    [FunctionName("Start")]
    public async Task<HttpResponseMessage> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter,
        ExecutionContext context,
        ILogger log)
    {
        string instanceId = await starter.StartNewAsync("Collector");    
        log.LogInformation($"Started Collector with ID = '{instanceId}'.");

        for (int i = 0; i < processorOptions.NbPartitions; i++)
        {
            instanceId = await starter.StartNewAsync<ProcessorInput>("Processor", 
                new ProcessorInput() { PartitionId = i, PostMode = true });
            log.LogInformation($"Started Processor orchestration with ID = '{instanceId}'.");
        }        

        return starter.CreateCheckStatusResponse(req, instanceId);
    }

    [FunctionName("Clear")]
    public async Task<IActionResult> HttpClear(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableEntityClient entityClient,
        [DurableClient] IDurableOrchestrationClient orchestrationClient,
        ExecutionContext context,
        ILogger log)
    {
        var tasks = new List<Task>();

        var instances = await orchestrationClient.ListInstancesAsync(new OrchestrationStatusQueryCondition() {  
            RuntimeStatus = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Running },
           }, CancellationToken.None);

        foreach(var instance in instances.DurableOrchestrationState.Where(x => x.Name == "Processor" || x.Name == "Collector"))
        {
            tasks.Add(orchestrationClient.TerminateAsync(instance.InstanceId, "Termination requested by user"));
        }

        var entities = await entityClient.ListEntitiesAsync(
            new EntityQuery() { EntityName="blobinfoentity" }, CancellationToken.None);

        foreach(var entity in entities.Entities)
        {
            tasks.Add(entityClient.SignalEntityAsync(entity.EntityId, "Clear"));
        }

        await Task.WhenAll(tasks);

        return new OkObjectResult("Clear complete");
    }    

    [FunctionName("Seed")]
    public async Task HttpSeed(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        ExecutionContext context,
        ILogger log)
    {
        var stateName = Enum.GetName(typeof(BlobInfo.ProcessState), BlobInfo.ProcessState.Unprocessed);

        for (int i = 0; i < 100000; i++)
        {
            var tasks = new List<Task>();
            for (int j=1; j<=6; j++)
            {
                var file = $"file-{i:0000000}-{j}.pdf";
                log.LogInformation(file);                
                
                var fi = new System.IO.FileInfo($@"..\..\..\Seed\Seed-{j}.pdf");
                tasks.Add(blobStorageService.UploadFileIfNewAndTag(fi.FullName, file, stateName));
            }

            await Task.WhenAll(tasks);
        }
    }

    [FunctionName("Diagnostics")]
    public async Task<IActionResult> HttpDiagnostics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient orchestrationClient,
        ExecutionContext context,
        ILogger log)
    {
        var instances = await orchestrationClient.ListInstancesAsync(new OrchestrationStatusQueryCondition() {  
            RuntimeStatus = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Running },
           }, CancellationToken.None);

        var inputs = new List<ProcessorInput>();
        foreach(var instance in instances.DurableOrchestrationState)
        {
            if (instance.Name == "Processor")
            {
                var input = instance.Input.ToObject<ProcessorInput>();
                inputs.Add(input);
            }
        }

        return new JsonResult(inputs);
    }
}