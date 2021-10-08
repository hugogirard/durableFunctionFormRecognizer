using System.Collections.Generic;
using Azure.AI.FormRecognizer.Models;

public class ProcessBlobResult
{   
    public BlobInfo Blob { get; set; }
    public string Exception { get; set; }
    public string OperationId { get; set; }
    public IEnumerable<RecognizedForm> Forms { get; set; }
}