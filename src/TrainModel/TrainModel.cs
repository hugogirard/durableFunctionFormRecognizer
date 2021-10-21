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

        [FunctionName("TrainCustomModel")]
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

        [FunctionName("GetCustomModel")]
        public async Task<IActionResult> GetModel([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
                                                   ILogger log)
        {
                string modelId = req.Query["modelId"];

                if (string.IsNullOrEmpty(modelId)) 
                {
                    return new BadRequestObjectResult("The query string modelId need to be present");
                }

                var response = await _trainingClient.GetCustomModelAsync(modelId);

                if (!response.GetRawResponse().Status.IsSuccessStatusCode())
                {                    
                    return new BadRequestObjectResult("Cannot retrieve the model");
                }

                return new OkObjectResult(response.Value);


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
