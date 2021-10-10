using Newtonsoft.Json;

public class BlobInfo
{
    public enum ProcessState
    {
        Unprocessed = 0,
        Processed = 1,
        Failed = 2
    }

    [JsonProperty("n")]
    public string BlobName {  get; set; }

    [JsonProperty("s")]
    public ProcessState State { get; set; }

    [JsonProperty("f")]
    public int TransientFailureCount { get; set; }    
}