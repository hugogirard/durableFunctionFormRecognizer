using Azure;
using Azure.AI.FormRecognizer.Training;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using TrainModel;

[assembly: FunctionsStartup(typeof(Startup))]

namespace TrainModel
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("FormRecognizerApiKey"));
            var endpoint = Environment.GetEnvironmentVariable("FormRecognizerEndpoint");
            var trainingClient = new FormTrainingClient(new Uri(endpoint), credential);

            var blobService = new BlobServiceClient(Environment.GetEnvironmentVariable("ModelStorageCnxString"));
            var containerClient = blobService.GetBlobContainerClient(Environment.GetEnvironmentVariable("ModelContainer"));

            builder.Services.AddSingleton(trainingClient);
            builder.Services.AddSingleton(containerClient);

        }
    }
}
