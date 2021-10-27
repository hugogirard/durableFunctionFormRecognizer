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
* including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.
*
* Please note: None of the conditions outlined in the disclaimer above will superseded the terms and conditions contained within the Premier Customer Services Description.
*
* DEMO POC - "AS IS"
*/
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SeederApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeederApp
{
    internal class Program
    {        
        static async Task Main(string[] args)
        {
            var result = new UploadResult();
         
            var bootstrapper = new Bootstrapper();
            ProgramConfiguration programConfiguration = bootstrapper.Start();

            // Start a timer to measure how long it takes to upload all the files.
            Stopwatch timer = Stopwatch.StartNew();

            try
            {
                int factor = programConfiguration.Factor;
                int nbrDocuments = programConfiguration.NbrDocuments;

                int left = 0;
                int index;

                if (nbrDocuments > factor)
                {
                    left = nbrDocuments % factor;
                    index = nbrDocuments / factor;
                }
                else
                {
                    index = 1;
                    factor = nbrDocuments;
                }

                var seederService = bootstrapper.GetSeederService;

                for (int i = 0; i < index; i++)
                {
                    var activityResult = await seederService.StartProcessingTasks(factor);                    
                    Aggregate(activityResult, result);
                }

                if (left > 0)
                {
                    var activityResult = await seederService.StartProcessingTasks(left);                    
                    Aggregate(activityResult, result);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            timer.Stop();

            Console.WriteLine("Process terminated");
            Console.WriteLine($"Uploaded {result.TotalDocumentProcess} files in {timer.Elapsed.TotalSeconds} seconds");
            Console.WriteLine($"TotalSuccessDocument: {result.TotalSuccessDocument}");
            Console.WriteLine($"TotalFailureDocument: {result.TotalFailureDocument}");
            
        }

        private static void Aggregate(IEnumerable<ActivityResult> results, UploadResult uploadResult)
        {
            uploadResult.TotalSuccessDocument += results.Count(r => r.IsSucces);
            uploadResult.TotalFailureDocument += results.Count(r => !r.IsSucces);

            Console.WriteLine("Batch Completed");
            Console.WriteLine("Total Processed");
            Console.WriteLine($"TotalSuccessDocument: {uploadResult.TotalSuccessDocument}");
            Console.WriteLine($"TotalFailureDocument: {uploadResult.TotalFailureDocument}");
        }
    }
}
