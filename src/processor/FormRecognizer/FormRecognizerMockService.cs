using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.Models;

public class FormRecognizerMockService : IFormRecognizerService
{
    private Uri serviceUrl;
    private IHttpClientFactory httpClientFactory;

    public FormRecognizerMockService(Uri serviceUrl, IHttpClientFactory httpClientFactory)
    {
        this.serviceUrl = serviceUrl;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<string> SubmitDocument(string modelId, Stream stream)
    {
        var client = httpClientFactory.CreateClient();
        var result = await client.GetAsync(serviceUrl);
        return Guid.NewGuid().ToString();
    }    

    public async Task<FormRecognizerResult> RetreiveResults(string operationId)
    {
        var client = httpClientFactory.CreateClient();
        var result = await client.GetAsync(serviceUrl);
        return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.CompletedWithoutResult };
    }
}