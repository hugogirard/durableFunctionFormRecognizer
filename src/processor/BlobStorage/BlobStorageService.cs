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
        var enumerator = blobServiceClient.FindBlobsByTagsAsync($"@container = '{blobContainerName}' AND status = '{stateName.ToLower()}'").
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
            tasks.Add(blobClient.SetTagsAsync(new Dictionary<string,string>() { { "status", Enum.GetName(typeof(BlobInfo.ProcessState), blob.State).ToLower() } }));
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
            await blobClient.SetTagsAsync(new Dictionary<string, string>() { { "status", state } });
        }, TaskContinuationOptions.AttachedToParent);        
    }

    public async Task<BlobInfo> GetBlob(string blobName)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(blobContainerName).GetBlobClient(blobName);
        var tags = await blobClient.GetTagsAsync();
        return new BlobInfo() { BlobName = blobName, State = Enum.Parse<BlobInfo.ProcessState>(tags.Value.Tags["status"], true) };
    }
}