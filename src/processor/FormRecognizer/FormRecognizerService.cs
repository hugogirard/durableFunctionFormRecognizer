using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;

public class FormRecognizerService : IFormRecognizerService
{
    private FormRecognizerClient formRecognizerClient;

    public FormRecognizerService(FormRecognizerClient formRecognizerClient)
    {
        this.formRecognizerClient = formRecognizerClient;
    }

    public async Task<string> SubmitDocument(string modelId, Stream stream)
    {
        try
        {
            return (await formRecognizerClient.StartRecognizeCustomFormsAsync(modelId, stream, new RecognizeCustomFormsOptions() { IncludeFieldElements = true })).Id;
        }
        catch (RequestFailedException ex)
        {
            if (ex.ErrorCode == "429") return null;
            throw;
        }
    }    

    public async Task<FormRecognizerResult> RetreiveResults(string operationId)
    {
        try
        {
            var operation = new RecognizeCustomFormsOperation(operationId, formRecognizerClient);
            await operation.UpdateStatusAsync(CancellationToken.None);
            if (operation.HasCompleted)
            {
                if (operation.HasValue) return new FormRecognizerResult() { Forms = operation.Value, Status = FormRecognizerResult.ResultStatus.CompletedWithResult };
                return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.CompletedWithoutResult };
            }
            return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.NotCompleted };;
        }
        catch (RequestFailedException ex)
        {
            if (ex.ErrorCode == "429") return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.TransientFailure };;
            throw;
        }            
    }
}