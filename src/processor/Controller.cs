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
* including attorneysâ€™ fees, that arise or result from the use or distribution of the Sample Code.
*
* Please note: None of the conditions outlined in the disclaimer above will superseded the terms and conditions contained within the Premier Customer Services Description.
*
* DEMO POC - "AS IS"
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

public class Controller
{
    private ProcessorOptions processorOptions;
    private IBlobStorageService blobStorageService;

    public Controller(ProcessorOptions processorOptions, IBlobStorageService blobStorageService)
    {
        this.processorOptions = processorOptions;
        this.blobStorageService = blobStorageService;
    }

    [FunctionName("Start")]
    public async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient client,
        ExecutionContext context,
        ILogger log)
    {
        var instances = await GetAllOrchestrations(client, new string[] { "Collector", "Processor" });

        if (instances.Any(x => x.Name == "Collector" && x.RuntimeStatus == OrchestrationRuntimeStatus.Running))
        {
            log.LogWarning($"Collector is already running...");
        }
        else
        {
            string instanceId = await client.StartNewAsync("Collector");    
            log.LogInformation($"Started Collector with ID = '{instanceId}'.");
        }

        var processorInstances = instances.Where(x => x.Name == "Processor" && x.RuntimeStatus == OrchestrationRuntimeStatus.Running);

        for (int i = 0; i < processorOptions.NbPartitions; i++)
        {
            if (processorInstances.Any(x => x.Input.ToObject<ProcessorInput>().PartitionId == i))
            {
                log.LogWarning($"Processor #{i} is already running...");
            }
            else
            {
                string instanceId = await client.StartNewAsync<ProcessorInput>("Processor", 
                    new ProcessorInput() { PartitionId = i });
                log.LogInformation($"Started Processor #{i} orchestration with ID = '{instanceId}'.");
            }
        }        

        return new OkObjectResult("Start complete");
    }

    [FunctionName("Clear")]
    public async Task<IActionResult> HttpClear(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableEntityClient entityClient,
        [DurableClient] IDurableOrchestrationClient orchestrationClient,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("Clearing state...");

        var tasks = new List<Task>();

        foreach(var instance in await GetAllOrchestrations(orchestrationClient, new string[] { "Collector", "Processor" }))
        { 
            if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Running)
            {
                log.LogInformation($"Terminating orchestration {instance.InstanceId}...");
                tasks.Add(orchestrationClient.TerminateAsync(instance.InstanceId, "Termination requested by user"));
            }
        }

        foreach(var entity in await GetAllEntities(entityClient, "blobinfoentity"))
        {
            log.LogInformation($"Clearing entity {entity.EntityId}...");
            tasks.Add(entityClient.SignalEntityAsync(entity.EntityId, "Clear"));
        }

        await Task.WhenAll(tasks);

        return new OkObjectResult("Clear complete");
    }

    [FunctionName("Diagnostics")]
    public async Task<IActionResult> HttpDiagnostics(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "patch", "delete")] HttpRequestMessage req,
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

        return new JsonResult(await GetAllOrchestrations(orchestrationClient, new string[] { "Collector", "Processor" }));
    }

    private static async Task<IEnumerable<DurableOrchestrationStatus>> GetAllOrchestrations(
        IDurableOrchestrationClient orchestrationClient, string[] names)
    {
        string continuationToken = null;
        var allInstances = new List<DurableOrchestrationStatus>();
        do
        {
            var condition = new OrchestrationStatusQueryCondition();
            condition.ContinuationToken = continuationToken;
            var result = await orchestrationClient.ListInstancesAsync(condition, CancellationToken.None);
            continuationToken = result.ContinuationToken;
            allInstances.AddRange(result.DurableOrchestrationState.Where(i => names.Any(n => n == i.Name)));
        }
        while (!String.IsNullOrEmpty(continuationToken));
        return allInstances;
    }

    private static async Task<IEnumerable<DurableEntityStatus>> GetAllEntities(
        IDurableEntityClient entityClient, string name)
    {
        string continuationToken = null;
        var allInstances = new List<DurableEntityStatus>();
        do
        {
            var condition = new EntityQuery();
            condition.EntityName = name;
            condition.ContinuationToken = continuationToken;
            var result = await entityClient.ListEntitiesAsync(condition, CancellationToken.None);
            continuationToken = result.ContinuationToken;
            allInstances.AddRange(result.Entities);
        }
        while (!String.IsNullOrEmpty(continuationToken));
        return allInstances;
    }
}