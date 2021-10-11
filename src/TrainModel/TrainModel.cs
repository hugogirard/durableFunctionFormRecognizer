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

namespace TrainModel
{
    public static class TrainModel
    {
        private static FormTrainingClient _trainingClient;

        public static FormTrainingClient FormTrainingClient  
        { 
            get
            {
                if (_trainingClient == null) 
                {
                    var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("FormRecognizerApiKey"));
                    var endpoint = Environment.GetEnvironmentVariable("FormRecognizerEndpoint");
                    _trainingClient = new FormTrainingClient(new Uri(endpoint),credential);

                }
                return _trainingClient;
            } 
        }

        [FunctionName("TrainModel")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
