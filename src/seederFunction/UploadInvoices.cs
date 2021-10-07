using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Seeder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        [FunctionName("UploadInvoiceWithTask")]
        public async Task<IEnumerable<ActivityResult>> UploadInvoiceWithTask(
            [ActivityTrigger] ActivityParameter parameter,
            [Blob("models/{parameter.ModelName}", FileAccess.Read, Connection = "DocumentStorage")] string myBlob,
            ILogger log)
        {
            var activityResults = new List<ActivityResult>();
            try
            {
                var tasks = new Queue<Task<ActivityResult>>();
                //var tasks = new List<Task>();
                foreach (var filename in parameter.Filenames)
                {
                    tasks.Enqueue(UploadBlob(filename, myBlob));
                    //tasks.Add(UploadBlob(filename, myBlob));
                }

                var result = await Task.WhenAll(tasks);
                return result;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
            }

            return activityResults;

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
                var blobClient = _blobContainerClient.GetBlobClient(parameter.Filenames.First());

                var responseUpload = await blobClient.UploadAsync(myBlob);

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
                log.LogError(ex.Message, ex);
                activityResult.Error = ex.Message;
            }

            return activityResult;

        }

        private async Task<ActivityResult> UploadBlob(string filename, string content)
        {
            var activityResult = new ActivityResult();
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(filename);
                
                var responseUpload = await blobClient.UploadAsync(new BinaryData(content));

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
    }
}
