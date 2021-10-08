using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SeederApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SeederApp
{
    internal class Program
    {
        private static BlobContainerClient _blobContainerClient;

        static async Task Main(string[] args)
        {
            var result = new UploadResult();
            
            // Start a timer to measure how long it takes to upload all the files.
            Stopwatch timer = Stopwatch.StartNew();
            
            try
            {
                var serviceClient = new BlobServiceClient("");
                _blobContainerClient = serviceClient.GetBlobContainerClient("documents");

                int factor = 100;
                int nbrDocuments = 1000;

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

                for (int i = 0; i < index; i++)
                {
                    var activityResult = await StartProcessingTasks(factor);
                    Console.WriteLine($"Processed batch documents");
                    Aggregate(activityResult, result);
                }

                if (left > 0)
                {
                    var activityResult = await StartProcessingTasks(left);
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

        private static async Task<IEnumerable<ActivityResult>> StartProcessingTasks(int index) 
        {                        
            int maxConcurrency = 4;

            BlobUploadOptions options = new BlobUploadOptions
            {
                TransferOptions = new StorageTransferOptions
                {
                    // Set the maximum number of workers that 
                    // may be used in a parallel transfer.
                    MaximumConcurrency = maxConcurrency,

                    // Set the maximum length of a transfer to 50MB.
                    MaximumTransferSize = 50 * 1024 * 1024
                }
            };

            var tasks = new Queue<Task<ActivityResult>>();

            string filepath = $"{Directory.GetCurrentDirectory()}\\Documents\\ceo.pdf";

            for (int i = 0; i < index; i++)
            {
                string filename = $"{Guid.NewGuid().ToString()}.pdf";
                tasks.Enqueue(UploadBlobAndTag(filename, filepath, options));
            }

            return await Task.WhenAll(tasks);

        }

        private static async Task<ActivityResult> UploadBlobAndTag(string filename,string filepath, BlobUploadOptions options) 
        {
            var activityResult = new ActivityResult();
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(filename);

                var responseUpload = await blobClient.UploadAsync(filepath, options);

                if (responseUpload.GetRawResponse().Status.IsSuccessStatusCode())
                {
                    var tags = new Dictionary<string, string>
                    {
                      { "status", "unprocessed" }
                    };
                    await blobClient.SetTagsAsync(tags);
                }
                else
                {
                    activityResult.Error = $"Cannot upload blob error code ${responseUpload.GetRawResponse().Status}";
                }

                activityResult.IsSucces = true;
            }
            catch (Exception ex)
            {                
                activityResult.Error = ex.Message;
            }

            return activityResult;
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
