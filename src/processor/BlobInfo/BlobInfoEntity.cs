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