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
        Blobs = new List<BlobInfo>();
    }

    [JsonProperty("b")]
    public List<BlobInfo> Blobs { get; set; }

    public void Clear() => Blobs.Clear();

    public int Count() => Blobs.Count;
    
    public IEnumerable<BlobInfo> Reserve(int maximumAmount)
    {
        if (Blobs.Count() == 0) return Enumerable.Empty<BlobInfo>();

        foreach(var blob in Blobs.Where(x => x.Reserved).ToArray())
        {
            Blobs.Remove(blob);
        }        

        var blobs = Blobs.Take(Math.Min(Blobs.Count, maximumAmount));
        foreach(var blob in blobs)
        {
            blob.Reserved = true;
        }
        return blobs;
    }

    public void AddIfNew(IEnumerable<BlobInfo> blobs) 
    {
        var blobIndex = Blobs.ToDictionary(x => x.BlobName);

        foreach(var blob in blobs)
        {
            if (!blobIndex.ContainsKey(blob.BlobName))
                Blobs.Add(blob);
        }
    }

    public static EntityId GetEntityId(int partitionId)
    {
        return new EntityId("BlobInfoEntity", $"{partitionId:000}");
    }

    [FunctionName(nameof(BlobInfoEntity))]
    public static Task Run([EntityTrigger] IDurableEntityContext context) => context.DispatchAsync<BlobInfoEntity>();
}