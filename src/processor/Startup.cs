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
using System.Net.Http;
using Azure.AI.FormRecognizer;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
	    var config = new ConfigurationBuilder()
            .AddEnvironmentVariables() 
            .Build();
        
        var storageAccountConnectionString = GetConfigValue<string>(config, "StorageAccountConnectionString", throwIfMissing: true);
        var blobContainerName = GetConfigValue<string>(config, "BlobContainerName", throwIfMissing: true);
        var formRecognizerKey = GetConfigValue<string>(config, "FormRecognizerKey", throwIfMissing: true);
        var formRecognizerEndpoint = GetConfigValue<string>(config, "FormRecognizerEndpoint", throwIfMissing: true);
        var formRecognizerModelId = GetConfigValue<string>(config, "FormRecognizerModelId", throwIfMissing: true);
        var cosmosEndpoint = GetConfigValue<string>(config, "CosmosEndpoint", throwIfMissing: true);
        var cosmosAuthKey = GetConfigValue<string>(config, "CosmosAuthKey", throwIfMissing: true);
        var cosmosDatabaseId = GetConfigValue<string>(config, "CosmosDatabaseId", throwIfMissing: true);
        var cosmosContainerId = GetConfigValue<string>(config, "CosmosContainerId", throwIfMissing: true);
        
        var batchSize = GetConfigValue<int>(config, "BatchSize", defaultValue: 5);
        var minBacklogSize = GetConfigValue<int>(config, "MinBacklogSize", defaultValue: 10);
        var nbPartitions = GetConfigValue<int>(config, "NbPartitions", defaultValue: 1);
        var maxRetries = GetConfigValue<int>(config, "MaxRetries", defaultValue: 3);
        var collectDelay = GetConfigValue<TimeSpan>(config, "CollectDelay", defaultValue: TimeSpan.FromSeconds(10));
        var noDataDelay = GetConfigValue<TimeSpan>(config, "NoDataDelay", defaultValue: TimeSpan.FromSeconds(10));
        var minProcessingTime = GetConfigValue<TimeSpan>(config, "MinProcessingTime", defaultValue: TimeSpan.FromSeconds(10));

        builder.Services.AddSingleton<IBlobStorageService>(_ => new BlobMockStorageService());
        // builder.Services.AddSingleton<IBlobStorageService>(sp => new BlobStorageService(
        //     blobContainerName, new BlobServiceClient(storageAccountConnectionString)));

        builder.Services.AddHttpClient();
        var serviceUrl = GetConfigValue<string>(config, "ServiceUrl", null);
        var serviceUri = String.IsNullOrEmpty(serviceUrl) ? null : new Uri(serviceUrl);
         builder.Services.AddSingleton<IFormRecognizerService>(sp => new FormRecognizerMockService(
             serviceUri, sp.GetService<IHttpClientFactory>()));
        // builder.Services.AddSingleton<IFormRecognizerService>(_ => {
        //     var formRecognizerClientOptions = new FormRecognizerClientOptions();
        //     formRecognizerClientOptions.Retry.MaxRetries = 0;
        //     return new FormRecognizerService(new FormRecognizerClient(new Uri(formRecognizerEndpoint), 
        //         new Azure.AzureKeyCredential(formRecognizerKey), formRecognizerClientOptions));
        // });

        builder.Services.AddSingleton<ICosmosService>(_ => new CosmosMockService());
        // builder.Services.AddSingleton<ICosmosService>(_ => {
        //     var client = new CosmosClient(cosmosEndpoint, cosmosAuthKey, new CosmosClientOptions() { AllowBulkExecution = true });
        //     return new CosmosService(client.GetContainer(cosmosDatabaseId, cosmosContainerId), 100 / nbPartitions);
        // });

        var partitionSize = (int)Math.Ceiling((double)batchSize / nbPartitions);

        builder.Services.AddSingleton<CollectorOptions>(_ => 
            new CollectorOptions() { BatchSize = batchSize, MinBacklogSize = minBacklogSize, 
                                     NbPartitions = nbPartitions, PartitionSize = partitionSize, CollectDelay = collectDelay,
                                     BlobContainerName = blobContainerName});
        
        builder.Services.AddSingleton<ProcessorOptions>(_ =>
            new ProcessorOptions() { NbPartitions =nbPartitions, PartitionSize = partitionSize, NoDataDelay = noDataDelay, MinProcessingTime = minProcessingTime,
                                     BlobContainerName = blobContainerName, MaxRetries = maxRetries, FormRecognizerModelId = formRecognizerModelId }); 
    }

    private static T GetConfigValue<T>(IConfigurationRoot config, string name, T defaultValue = default(T), bool throwIfMissing = false)
    {
        if (config.GetSection(name).Exists())
        {
            return config.GetValue<T>(name);
        }
        else
        {
            if (throwIfMissing)
                throw new InvalidOperationException($"{name} must be specified in config");
            else
                return defaultValue;            
        }
    }
}