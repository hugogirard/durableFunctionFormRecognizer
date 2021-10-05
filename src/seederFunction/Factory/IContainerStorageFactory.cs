using Azure.Storage.Blobs;

namespace Seeder.Factory
{
    public interface IContainerStorageFactory
    {
        BlobContainerClient CreateClient(string containerName);
    }
}