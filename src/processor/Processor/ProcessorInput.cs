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
* including attorneysâ€™ fees, that arise or result from the use or distribution of the Sample Code.
*
* Please note: None of the conditions outlined in the disclaimer above will superseded the terms and conditions contained within the Premier Customer Services Description.
*
* DEMO POC - "AS IS"
*/
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