using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SeederApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeederApp.Service
{
    public class SeederService : ISeederService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly SemaphoreSlim _gate;
        private readonly int _maxConcurrency;        

        public SeederService(BlobContainerClient containerClient,ProgramConfiguration configuration)
        {
            _containerClient = containerClient;
            _gate = new SemaphoreSlim(configuration.MaxConcurrency);
            _maxConcurrency = configuration.MaxConcurrency;
        }

        public async Task<IEnumerable<ActivityResult>> StartProcessingTasks(int index)
        {            
            BlobUploadOptions options = new BlobUploadOptions
            {
                TransferOptions = new StorageTransferOptions
                {
                    // Set the maximum number of workers that 
                    // may be used in a parallel transfer.
                    MaximumConcurrency = _maxConcurrency,

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

        private async Task<ActivityResult> UploadBlobAndTag(string filename, string filepath, BlobUploadOptions options)
        {
            await _gate.WaitAsync();

            var activityResult = new ActivityResult();
            try
            {
                var blobClient = _containerClient.GetBlobClient(filename);

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
            finally 
            {
                _gate.Release();
            }
            
            return activityResult;
        }
    }
}
