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
                    Console.WriteLine($"Processed batch documents");
                    Aggregate(activityResult, result);
                }

                if (left > 0)
                {
                    var activityResult = await seederService.StartProcessingTasks(left);
                    Console.WriteLine($"Processed batch documents");
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

            Console.WriteLine("Aggregate");
            Console.WriteLine($"TotalSuccessDocument: {results.Count(r => r.IsSucces)}");
            Console.WriteLine($"TotalFailureDocument: {results.Count(r => !r.IsSucces)}");
        }
    }
}
