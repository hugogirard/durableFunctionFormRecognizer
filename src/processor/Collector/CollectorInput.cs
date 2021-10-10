using System;
using Newtonsoft.Json;

public class CollectorInput
{   
    [JsonProperty("c")]
    public string ContinuationToken { get; set; }

    [JsonProperty("p")]
    public string PreviousInstanceId { get; set; }
}