using System.Collections.Generic;
using Newtonsoft.Json;

public class ProcessInput
{    

    [JsonProperty("p")]
    public int PartitionId { get; set; }

    [JsonProperty("e")]
    public IEnumerable<BlobInfo> Blobs { get; set; }
}