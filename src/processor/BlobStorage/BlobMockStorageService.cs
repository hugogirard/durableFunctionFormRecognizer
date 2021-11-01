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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class BlobMockStorageService : IBlobStorageService
{
    public Task<CollectorOutput> GetUnprocessedBlobs(int batchSize, string continuationToken)
    {
        var blobs = new List<BlobInfo>();

        if (continuationToken == null)
            continuationToken = "0";

        int start = int.Parse(continuationToken);
        for (int i=start; i<start+batchSize; i++)
        {
            blobs.Add(new BlobInfo() { BlobName = $"{i:0000}", State = BlobInfo.ProcessState.Unprocessed });
        }

        return Task.FromResult(new CollectorOutput() { Blobs = blobs, ContinuationToken = (start+batchSize).ToString() });
    }

    public Task UpdateState(IEnumerable<BlobInfo> blobs)
    {
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadStream(string blobName)
    {
        return Task.FromResult(Stream.Null);
    }

    public Task<BlobInfo> GetBlob(string blobName)
    {
        return Task.FromResult(new BlobInfo() { BlobName = blobName, State = BlobInfo.ProcessState.Processed });
    }
}