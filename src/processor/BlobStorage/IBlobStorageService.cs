using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public interface IBlobStorageService
{
    public Task<CollectorOutput> GetUnprocessedBlobs(int batchSize, string continuationToken);
    public Task UpdateState(IEnumerable<BlobInfo> blobs);    
    public Task<Stream> DownloadStream(string blobName);
    public Task UploadStream(string blobName, Stream stream, bool overwite = false);
    public Task UploadFileIfNewAndTag(string sourceFile, string targetFile, string state);
}