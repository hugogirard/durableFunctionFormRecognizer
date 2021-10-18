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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Microsoft.Extensions.Logging;

public class FormRecognizerService : IFormRecognizerService
{
    private FormRecognizerClient formRecognizerClient;

    public FormRecognizerService(FormRecognizerClient formRecognizerClient)
    {
        this.formRecognizerClient = formRecognizerClient;
    }

    public async Task<string> SubmitDocument(string modelId, Stream stream, ILogger log)
    {
        try
        {
            return (await formRecognizerClient.StartRecognizeCustomFormsAsync(modelId, stream, new RecognizeCustomFormsOptions() { IncludeFieldElements = true })).Id;
        }
        catch (RequestFailedException ex)
        {
            if (ex.ErrorCode == "429") 
            {
                log.LogWarning("Form Recognizer call was throttled on POST...");
                throw new TransientFailureException("Form Recognizer call was throttled on POST...");
            }
            throw;
        }
    }    

    public async Task<FormRecognizerResult> RetreiveResults(string operationId, ILogger log)
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
            if (ex.ErrorCode == "429") 
            {
                log.LogWarning("Form Recognizer call was throttled on GET...");
                throw new TransientFailureException("Form Recognizer call was throttled on GET...");
            }
            throw;
        }            
    }
}