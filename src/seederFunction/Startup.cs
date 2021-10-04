using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(Seeder.Startup))]

namespace Seeder
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var serviceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("DocumentStorage"));
            var containerClient = serviceClient.GetBlobContainerClient("documents");

            builder.Services.AddSingleton(typeof(BlobContainerClient), containerClient);
        }
    }
}
