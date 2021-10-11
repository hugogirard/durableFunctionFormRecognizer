using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.AI.FormRecognizer.Training;
using Azure;
using Azure.Storage.Blobs;
using System.Collections.Generic;

namespace TrainModel
{
    public class TrainModel
    {
        private readonly FormTrainingClient _trainingClient;
        private BlobContainerClient _blobContainerClient;

        public TrainModel(FormTrainingClient trainingClient, BlobContainerClient blobContainerClient)
        {
            _trainingClient = trainingClient;
            _blobContainerClient = blobContainerClient;
        }

        [FunctionName("TrainModel")]
        public async Task<IActionResult> TrainCustomModel(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            try
            {
                string modelName = req.Query["modelName"];

                if (string.IsNullOrEmpty(modelName)) 
                {
                    return new BadRequestObjectResult("The query string modelName need to be present");
                }

                var sas = _blobContainerClient.GenerateSasUri(Azure.Storage.Sas.BlobContainerSasPermissions.All,
                                              DateTime.UtcNow.AddMinutes(15));
                
                var response = await _trainingClient.StartTrainingAsync(sas, useTrainingLabels: true, modelName);
                
                CustomFormModel model = await response.WaitForCompletionAsync();

                return new OkObjectResult(model);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new ObjectResult("Internal Server Error") { StatusCode = 500 };
            }
        }

        [FunctionName("GetCustomModels")]
        public async Task<IActionResult> GetCustomModels([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
                                                         ILogger log)
        {

            try
            {
                var models = new List<CustomFormModelInfo>();
                var customModels = _trainingClient.GetCustomModelsAsync();
                await foreach (var customModel in customModels)
                {
                    models.Add(customModel);
                }

                return new OkObjectResult(customModels);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new ObjectResult("Internal Server Error") { StatusCode = 500 };
            }



        }

        [FunctionName("DeleteCustomModel")]
        public async Task<IActionResult> DeleteCustomModel([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
                                                            ILogger log)
        {

            try
            {
                string modelId = req.Query["modelId"];

                if (string.IsNullOrEmpty(modelId))
                {
                    return new BadRequestObjectResult("The query string modelId need to be present");
                }

                var response = await _trainingClient.DeleteModelAsync(modelId);

                if (response.Status.IsSuccessStatusCode())
                {
                    return new OkResult();
                }

                return new ObjectResult(string.Empty) { StatusCode = response.Status };
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return new ObjectResult("Internal Server Error") { StatusCode = 500 };
            }

        }


    }
}
