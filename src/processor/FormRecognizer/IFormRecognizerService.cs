using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IFormRecognizerService
{
    public Task<string> SubmitDocument(string modelId, Stream stream, ILogger log);
    public Task<FormRecognizerResult> RetreiveResults(string operationId, ILogger log);
}