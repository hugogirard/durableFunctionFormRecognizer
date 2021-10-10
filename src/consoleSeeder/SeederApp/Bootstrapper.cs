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

            //string containerEndpoint = "https://strdtcq2dqmjiz542.blob.core.windows.net/documents";

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
