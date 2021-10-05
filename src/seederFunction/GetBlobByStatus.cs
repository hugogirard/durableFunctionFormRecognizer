using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Seeder
{
    /// <summary>
    /// Get blob by status
    /// </summary>
    public class GetBlobByStatus
    {
        readonly IEnumerable<string> VALID_STATUS = new List<string>{ "unprocessed" };
        private readonly BlobServiceClient _blobServiceClient;

        public GetBlobByStatus(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        [FunctionName("GetBlobByStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string status = req.Query["status"];

            if (string.IsNullOrEmpty(status)) 
            {
                return new BadRequestObjectResult("Status query string is missing");
            }


            if (VALID_STATUS.Any(x => x == status.ToLower()))
            {
                string query = @$"""status"" = '{status}'";
                List<TaggedBlobItem> blobs = new List<TaggedBlobItem>();
                await foreach (TaggedBlobItem taggedBlobItem in _blobServiceClient.FindBlobsByTagsAsync(query))
                {
                    blobs.Add(taggedBlobItem);
                }
                return new OkObjectResult(blobs);
            }
            else 
            {
                return new BadRequestObjectResult("Invalid status passed in parameter");
            }

            
        }
    }
}
