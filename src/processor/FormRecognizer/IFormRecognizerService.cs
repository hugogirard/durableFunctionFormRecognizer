using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.Models;

public interface IFormRecognizerService
{
    public Task<string> SubmitDocument(string modelId, Stream stream);
    public Task<FormRecognizerResult> RetreiveResults(string operationId);
}