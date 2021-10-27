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
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class BlobInfoEntity
{
    [JsonIgnore]
    private ILogger logger;

    public BlobInfoEntity(ILogger<BlobInfoEntity> logger)
    {
        this.logger = logger;
        Backlog = new List<BlobInfo>();
        Partitions = new Dictionary<int, List<BlobInfo>>();
        Cache = new List<BlobInfo>();
    }

    [JsonProperty("b")]
    public List<BlobInfo> Backlog { get; set; }

    [JsonProperty("p")]
    public Dictionary<int, List<BlobInfo>> Partitions { get; set; }

    [JsonProperty("c")]
    public List<BlobInfo> Cache { get; set; }    

    public void Clear()
    {        
        Backlog.Clear();
        Partitions.Clear();
        Cache.Clear();
    }

    public int CountBacklog() => Backlog.Count;
    
    public IEnumerable<BlobInfo> Reserve((int partitionId, int maximumAmount) input)
    {
        if (!Partitions.ContainsKey(input.partitionId))
        {
            Partitions[input.partitionId] = new List<BlobInfo>();
        }

        if (Partitions[input.partitionId].Count == 0)
        {
            if (Backlog.Count == 0) return Enumerable.Empty<BlobInfo>();             

            var amount = Math.Min(Backlog.Count, input.maximumAmount);
            Partitions[input.partitionId].AddRange(Backlog.Take(amount));
            Backlog.RemoveRange(0, amount);
        }

        return Partitions[input.partitionId];
    }

    public void ClearReserved((int partitionId, IEnumerable<BlobInfo> blobs) input)
    {
        var cacheIndex = Cache.ToDictionary(x => x.BlobName);

        var now = DateTime.Now;
        foreach(var blob in input.blobs)
        {
            if (cacheIndex.ContainsKey(blob.BlobName))
            {
                logger.LogWarning($"[BlobInfoEntity] Blob {blob.BlobName} was already present in the cache...");
            }
            else
            {
                blob.StateChangeTime = now;
                Cache.Add(blob);                
            }
        }

        if (Partitions.ContainsKey(input.partitionId))
            Partitions[input.partitionId].Clear();
    }

    public int AddToBacklog(IEnumerable<BlobInfo> blobs) 
    {
        var backlogIndex = Backlog.ToDictionary(x => x.BlobName);
        var partitionIndex = Partitions.Values.SelectMany(x => x).ToDictionary(x => x.BlobName);
        var cacheIndex = Cache.ToDictionary(x => x.BlobName);

        foreach(var blob in blobs)
        {
            if (cacheIndex.ContainsKey(blob.BlobName))
                logger.LogInformation($"Cache hit for {blob.BlobName}");

            if (!backlogIndex.ContainsKey(blob.BlobName) && 
                !partitionIndex.ContainsKey(blob.BlobName) &&
                !cacheIndex.ContainsKey(blob.BlobName))
            {
                Backlog.Add(blob);
            }
        }
        return Backlog.Count;
    }

    public IEnumerable<BlobInfo> GetCache() => Cache;

    public void RemoveFromCache(IEnumerable<BlobInfo> blobs)
    {
        var cacheIndex = Cache.ToDictionary(x => x.BlobName);

        foreach (var blob in blobs)
        {
            if (cacheIndex.ContainsKey(blob.BlobName))
                Cache.Remove(cacheIndex[blob.BlobName]);
        }        
    }    

    public static readonly EntityId EntityId = new EntityId("BlobInfoEntity", "1");

    [FunctionName(nameof(BlobInfoEntity))]
    public static Task Run([EntityTrigger] IDurableEntityContext context) => context.DispatchAsync<BlobInfoEntity>();
}