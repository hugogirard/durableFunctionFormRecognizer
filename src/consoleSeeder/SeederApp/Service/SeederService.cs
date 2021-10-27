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

            string[] files = new []
            {
                 $"{Directory.GetCurrentDirectory()}\\Documents\\generic1.pdf",
                 $"{Directory.GetCurrentDirectory()}\\Documents\\generic2.pdf",
                 $"{Directory.GetCurrentDirectory()}\\Documents\\generic3.pdf"
            };

            var random = new Random();

            for (int i = 0; i < index; i++)
            {
                string filename = $"{Guid.NewGuid().ToString()}.pdf";
                int idx = random.Next(0,2);
                string filepath = files[idx];
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
