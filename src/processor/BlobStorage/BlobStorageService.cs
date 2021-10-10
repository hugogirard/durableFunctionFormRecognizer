using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

public class BlobStorageService : IBlobStorageService
{
    private string blobContainerName;
    private BlobServiceClient blobServiceClient;

    public BlobStorageService(string blobContainerName, BlobServiceClient blobServiceClient)
    {
        this.blobContainerName = blobContainerName;
        this.blobServiceClient = blobServiceClient;
    }

    public async Task<CollectorOutput> GetUnprocessedBlobs(int batchSize, string continuationToken)
    {
        var blobs = new List<BlobInfo>();
        var stateName = Enum.GetName(typeof(BlobInfo.ProcessState), BlobInfo.ProcessState.Unprocessed);
        var enumerator = blobServiceClient.FindBlobsByTagsAsync($"@container = '{blobContainerName}' AND State = '{stateName}'").
            AsPages(continuationToken, batchSize).GetAsyncEnumerator();
        
        continuationToken = null;
        if (await enumerator.MoveNextAsync())
        {
            continuationToken = enumerator.Current.ContinuationToken;
            blobs.AddRange(enumerator.Current.Values.Select(x => new BlobInfo() { BlobName = x.BlobName, State = BlobInfo.ProcessState.Unprocessed }).ToArray());
        }        

        return new CollectorOutput() { Blobs = blobs, ContinuationToken = continuationToken };
    }

    public async Task UpdateState(IEnumerable<BlobInfo> blobs)
    {
        var tasks = new List<Task>();
        foreach(var blob in blobs)
        {
            var blobClient = blobServiceClient.GetBlobContainerClient(blobContainerName).GetBlobClient(blob.BlobName);
            tasks.Add(blobClient.SetTagsAsync(new Dictionary<string,string>() { { "State", Enum.GetName(typeof(BlobInfo.ProcessState), blob.State) } }));
        }
        await Task.WhenAll(tasks);
    }

    public async Task<Stream> DownloadStream(string blobName)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(blobContainerName).GetBlobClient(blobName);
        var content = await blobClient.DownloadContentAsync();
        return content.Value.Content.ToStream();
    }

    public async Task UploadStream(string blobName, Stream stream, bool overwite = false)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(blobContainerName).GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: overwite);
    }

    public Task UploadFileIfNewAndTag(string sourceFile, string targetFile, string state)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(blobContainerName).GetBlobClient(targetFile);
        return blobClient.ExistsAsync().ContinueWith(async e => {
            if (!e.Result)                        
            {                        
                await blobClient.UploadAsync(sourceFile);
            }
            await blobClient.SetTagsAsync(new Dictionary<string, string>() { { "State", state } });
        }, TaskContinuationOptions.AttachedToParent);        
    }
}