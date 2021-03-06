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
using System.Text.Json;
    //
    // Summary:
    //     Represents the status of a durable orchestration instance.
    //
    // Remarks:
    //     An external client can fetch the status of an orchestration instance using Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationClient.GetStatusAsync(System.String,System.Boolean,System.Boolean,System.Boolean).
    public class DurableOrchestrationStatus
    {
        public enum OrchestrationRuntimeStatus
        {
            //
            // Summary:
            //     The status of the orchestration could not be determined.
            Unknown = -1,
            //
            // Summary:
            //     The orchestration is running (it may be actively running or waiting for input).
            Running = 0,
            //
            // Summary:
            //     The orchestration ran to completion.
            Completed = 1,
            //
            // Summary:
            //     The orchestration completed with ContinueAsNew as is in the process of restarting.
            ContinuedAsNew = 2,
            //
            // Summary:
            //     The orchestration failed with an error.
            Failed = 3,
            //
            // Summary:
            //     The orchestration was canceled.
            Canceled = 4,
            //
            // Summary:
            //     The orchestration was terminated via an API call.
            Terminated = 5,
            //
            // Summary:
            //     The orchestration was scheduled but has not yet started.
            Pending = 6
        }

        //
        // Summary:
        //     Gets the name of the queried orchestrator function.
        //
        // Value:
        //     The orchestrator function name.
        public string Name { get; set; }
        //
        // Summary:
        //     Gets the ID of the queried orchestration instance.
        //
        // Value:
        //     The unique ID of the instance.
        //
        // Remarks:
        //     The instance ID is generated and fixed when the orchestrator function is scheduled.
        //     It can be either auto-generated, in which case it is formatted as a GUID, or
        //     it can be user-specified with any format.
        public string InstanceId { get; set; }
        //
        // Summary:
        //     Gets the time at which the orchestration instance was created.
        //
        // Value:
        //     The instance creation time in UTC.
        //
        // Remarks:
        //     If the orchestration instance is in the Microsoft.Azure.WebJobs.Extensions.DurableTask.OrchestrationRuntimeStatus.Pending
        //     status, this time represents the time at which the orchestration instance was
        //     scheduled.
        public DateTime CreatedTime { get; set; }
        //
        // Summary:
        //     Gets the time at which the orchestration instance last updated its execution
        //     history.
        //
        // Value:
        //     The last-updated time in UTC.
        public DateTime LastUpdatedTime { get; set; }
        //
        // Summary:
        //     Gets the input of the orchestrator function instance.
        //
        // Value:
        //     The input as either a JToken or null if no input was provided.
        public JsonElement Input { get; set; }
        //
        // Summary:
        //     Gets the output of the queried orchestration instance.
        //
        // Value:
        //     The output as either a JToken object or null if it has not yet completed.
        public JsonElement Output { get; set; }
        //
        // Summary:
        //     Gets the runtime status of the queried orchestration instance.
        //
        // Value:
        //     Expected values include `Running`, `Pending`, `Failed`, `Canceled`, `Terminated`,
        //     `Completed`.
        public OrchestrationRuntimeStatus RuntimeStatus { get; set; }
        //
        // Summary:
        //     Gets the custom status payload (if any) that was set by the orchestrator function.
        //
        // Value:
        //     The custom status as either a JToken object or null if no custom status has been
        //     set.
        //
        // Remarks:
        //     Orchestrator functions can set a custom status using Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationContext.SetCustomStatus(System.Object).
        public JsonElement CustomStatus { get; set; }
        //
        // Summary:
        //     Gets the execution history of the orchestration instance.
        //
        // Value:
        //     The output as a JArray object or null.
        //
        // Remarks:
        //     The history log can be large and is therefore null by default. It is populated
        //     only when explicitly requested in the call to Microsoft.Azure.WebJobs.Extensions.DurableTask.IDurableOrchestrationClient.GetStatusAsync(System.String,System.Boolean,System.Boolean,System.Boolean).
        // public JArray History { get; set; }

        public T GetInputObject<T>() => JsonSerializer.Deserialize<T>(Input.GetRawText());
    }