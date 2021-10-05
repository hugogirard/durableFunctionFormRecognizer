using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Seeder.Factory
{
    public class ContainerStorageFactory : IContainerStorageFactory
    {
        private Dictionary<string, BlobContainerClient> _dictionary;
        private readonly BlobServiceClient _serviceClient;

        public ContainerStorageFactory(string connectionString)
        {
            _dictionary = new Dictionary<string, BlobContainerClient>();
            _serviceClient = new BlobServiceClient(connectionString);
        }

        public BlobContainerClient CreateClient(string containerName)
        {
            if (_dictionary.ContainsKey(containerName))
            {
                return _dictionary[containerName];
            }
            
            var containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _dictionary.Add(containerName, containerClient);

            return containerClient;
        }
    }
}
