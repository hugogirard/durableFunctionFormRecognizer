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

public class BlobInfoEntity
{
    public BlobInfoEntity()
    {
        // Using a dictionary to reduce serialization size
        Blobs = new Dictionary<bool, List<BlobInfo>>();
        Blobs[true] = new List<BlobInfo>();
        Blobs[false] = new List<BlobInfo>();
    }

    [JsonProperty("b")]
    public Dictionary<bool, List<BlobInfo>> Blobs { get; set; }

    private List<BlobInfo> Reserved => Blobs[true];
    private List<BlobInfo> Unreserved => Blobs[false];
    private IEnumerable<BlobInfo> AllBlobs => Unreserved.Concat(Reserved); 

    public void Clear() => Blobs.Clear();

    public int Count() => Reserved.Count + Unreserved.Count;
    
    public IEnumerable<BlobInfo> Reserve(int maximumAmount)
    {
        if (!Reserved.Any())
        {
            if (!Unreserved.Any()) return Enumerable.Empty<BlobInfo>();             

            var amount = Math.Min(Unreserved.Count, maximumAmount);
            Reserved.AddRange(Unreserved.Take(amount));
            Unreserved.RemoveRange(0, amount);
        }

        return Reserved;
    }

    public void ClearReserved()
    {
        Reserved.Clear();
    }

    public void AddIfNew(IEnumerable<BlobInfo> blobs) 
    {
        var blobIndex = AllBlobs.ToDictionary(x => x.BlobName);

        foreach(var blob in blobs)
        {
            if (!blobIndex.ContainsKey(blob.BlobName))
                Unreserved.Add(blob);
        }
    }

    public static EntityId GetEntityId(int partitionId)
    {
        return new EntityId("BlobInfoEntity", $"{partitionId:000}");
    }

    [FunctionName(nameof(BlobInfoEntity))]
    public static Task Run([EntityTrigger] IDurableEntityContext context) => context.DispatchAsync<BlobInfoEntity>();
}