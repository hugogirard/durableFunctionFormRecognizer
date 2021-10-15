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
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeederApp.Models;
using SeederApp.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeederApp
{
    public class Bootstrapper
    {
        private readonly ServiceProvider _serviceProvider;

        public ISeederService GetSeederService => _serviceProvider.GetService<ISeederService>();

        public Bootstrapper()
        {

            var builder = new ConfigurationBuilder().AddJsonFile("appSettings.json", false, false);

#if DEBUG
            builder.AddUserSecrets<Program>();
#endif

            var config = builder.Build();

            var programConfiguration = new ProgramConfiguration();
            config.GetSection(programConfiguration.SECTION_NAME).Bind(programConfiguration);

            //string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
            //                                programConfiguration.StorageAccountName,
            //                                programConfiguration.StorageContainerName);

            //string containerEndpoint = "";

            // Using MSI to connect to avoid any credential leeking in configuration
            var serviceBlob = new BlobServiceClient(programConfiguration.StorageCnxString);
            var blobContainerClient = serviceBlob.GetBlobContainerClient(programConfiguration.StorageContainerName);
            //var blobContainerClient = new BlobContainerClient(new Uri(containerEndpoint), new DefaultAzureCredential());

            _serviceProvider = new ServiceCollection()
                                     .AddSingleton(blobContainerClient)
                                     .AddSingleton(programConfiguration)
                                     .AddSingleton<ISeederService, SeederService>()
                                     .BuildServiceProvider();
        }

        public ProgramConfiguration Start() 
        {
            return _serviceProvider.GetService<ProgramConfiguration>();
        }
        
    }
}
