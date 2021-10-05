using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Seeder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Seeder
{
    public class UploadInvoices
    {
        private readonly BlobContainerClient _blobContainerClient;

        public UploadInvoices(BlobContainerClient blobContainerClient)
        {
            _blobContainerClient = blobContainerClient;
        }

        [FunctionName("UploadInvoice")]
        public async Task<ActivityResult> UploadInvoice(
            [ActivityTrigger] ActivityParameter parameter,
            [Blob("models/{parameter.ModelName}", FileAccess.Read, Connection = "DocumentStorage")] Stream myBlob,
            ILogger log)
        {
            var activityResult = new ActivityResult();
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(parameter.Filename);

                var responseUpload = await blobClient.UploadAsync(myBlob);

                if (responseUpload.GetRawResponse().Status.IsSuccessStatusCode()) 
                {
                    //var appendBlobClient = _blobContainerClient.GetAppendBlobClient(parameter.Filename);
                    //var appendOptions = new AppendBlobCreateOptions();
                    //appendOptions.Tags = new Dictionary<string, string>
                    //{
                    //  { "status", "unprocessed" }
                    //};

                    //var responseAppend = await appendBlobClient.CreateAsync(appendOptions);
                    //if (responseAppend.GetRawResponse().Status != 200) 
                    //{
                    //    activityResult.Error = $"Cannot append tag to blob ${parameter.Filename} - StatusCode: ${responseAppend.GetRawResponse().Status}";
                    //}
                }
                else 
                {
                    activityResult.Error = $"Cannot upload blob error code ${responseUpload.GetRawResponse().Status}";
                }

                activityResult.IsSucces = true;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message,ex);                
                activityResult.Error = ex.Message;
            }

            return activityResult;

        }

    }
}
