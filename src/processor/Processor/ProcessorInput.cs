using System;
using Newtonsoft.Json;

public class ProcessorInput
{   
    public class Statistics
    {
        [JsonProperty("p")]
        public Int64 TotalProcessed { get; set; }
        [JsonProperty("f")]
        public Int64 TotalFailed { get; set; }
        [JsonProperty("t")]
        public Int64 TotalTransientFailures { get; set; }
    }

    [JsonProperty("i")]
    public int PartitionId { get; set; }

    [JsonProperty("p")]
    public string PreviousInstanceId { get; set; }
    
    [JsonProperty("s")]
    public Statistics Stats { get; set; }

    public ProcessorInput()
    {
        Stats = new Statistics();
    }
}