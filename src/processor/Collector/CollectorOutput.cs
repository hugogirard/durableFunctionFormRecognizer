using System.Collections.Generic;
using Newtonsoft.Json;

public class CollectorOutput
{    

    [JsonProperty("b")]
    public IEnumerable<BlobInfo> Blobs { get; set; }

    [JsonProperty("c")]
    public string ContinuationToken { get; set; }
}