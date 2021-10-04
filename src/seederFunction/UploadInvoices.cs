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
        private readonly BlobContainerClient _containerClient;

        public UploadInvoices(BlobContainerClient containerClient)
        {
            _containerClient = containerClient;
        }

        [FunctionName("UploadInvoice")]
        public async Task<ActivityResult> SayHello([ActivityTrigger] string filename, ILogger log)
        {
            var activityResult = new ActivityResult();

            try
            {
                var blobClient = _containerClient.GetAppendBlobClient(filename);
                var appendOptions = new AppendBlobCreateOptions();
                appendOptions.Tags = new Dictionary<string, string>
                {
                  { "status", "unprocessed" }
                };

                await blobClient.CreateAsync(appendOptions);
                using (FileStream fs = File.Open(@"Invoice_Template.pdf",FileMode.Open))
                {                    
                    await blobClient.AppendBlockAsync(fs);
                }
                
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
