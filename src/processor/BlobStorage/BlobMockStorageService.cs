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

    public Task UploadStream(string blobName, Stream stream, bool overwite = false)
    {
        return Task.CompletedTask;
    }

    public Task UploadFileIfNewAndTag(string sourceFile, string targetFile, string state)
    {        
        return Task.CompletedTask;
    }
}