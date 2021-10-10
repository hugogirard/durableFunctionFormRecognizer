using System;
using System.Text.Json.Serialization;

public class CollectorInput
{   
    [JsonPropertyName("c")]
    public string ContinuationToken { get; set; }

    [JsonPropertyName("p")]
    public string PreviousInstanceId { get; set; }
}