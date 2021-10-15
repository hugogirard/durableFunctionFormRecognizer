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
                new ProcessorInput() { PartitionId = i });
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "patch", "delete")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient orchestrationClient,
        ExecutionContext context,
        ILogger log)
    {
        if (req.Method == HttpMethod.Post)
        {
            var instanceId = await req.Content.ReadAsStringAsync();
            await orchestrationClient.RestartAsync(instanceId);
        }

        if (req.Method == HttpMethod.Patch)
        {
            var instanceId = await req.Content.ReadAsStringAsync();
            await orchestrationClient.TerminateAsync(instanceId, "Termination requested by user");
        }

        if (req.Method == HttpMethod.Delete)
        {
            var instanceId = await req.Content.ReadAsStringAsync();
            await orchestrationClient.PurgeInstanceHistoryAsync(instanceId);
        }

        var instances = await orchestrationClient.ListInstancesAsync(
            new OrchestrationStatusQueryCondition(), CancellationToken.None);

        return new JsonResult(instances.DurableOrchestrationState.Where(
            x => x.Name == "Collector" || x.Name == "Processor"));
    }
}