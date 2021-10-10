﻿using System;
using System.Text.Json.Serialization;

public class ProcessorInput
{
    public class Statistics
    {
        [JsonPropertyName("p")]
        public Int64 TotalProcessed { get; set; }
        [JsonPropertyName("f")]
        public Int64 TotalFailed { get; set; }
        [JsonPropertyName("t")]
        public Int64 TotalTransientFailures { get; set; }
    }

    [JsonPropertyName("i")]
    public int PartitionId { get; set; }

    [JsonPropertyName("p")]
    public string PreviousInstanceId { get; set; }

    [JsonPropertyName("s")]
    public Statistics Stats { get; set; }

    public ProcessorInput()
    {
        Stats = new Statistics();
    }
}