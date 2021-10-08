using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class FormRecognizerMockService : IFormRecognizerService
{
    private Uri serviceUrl;
    private IHttpClientFactory httpClientFactory;

    public FormRecognizerMockService(Uri serviceUrl, IHttpClientFactory httpClientFactory)
    {
        this.serviceUrl = serviceUrl;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<string> SubmitDocument(string modelId, Stream stream, ILogger log)
    {
        // Simulate failures and latency
        var rnd = new Random();
        if (rnd.Next(1, 20) == 1) throw new ApplicationException("Simulated failure...");
        if (rnd.Next(1, 20) == 2) { Thread.Sleep(2000); log.LogInformation("Simulated delay..."); }
        if (rnd.Next(1, 20) == 3)
        {
            log.LogWarning("Simulated transient failure...");
            return null;
        }      

        var client = httpClientFactory.CreateClient();
        var result = await client.GetAsync(serviceUrl);
        return Guid.NewGuid().ToString();
    }    

    public async Task<FormRecognizerResult> RetreiveResults(string operationId, ILogger log)
    {
        // Simulate failures and latency
        var rnd = new Random();
        if (rnd.Next(1, 20) == 1) throw new ApplicationException("Simulated failure...");
        if (rnd.Next(1, 20) == 2) { Thread.Sleep(2000); log.LogInformation("Simulated delay..."); }
        if (rnd.Next(1, 20) == 3)
        {
            log.LogInformation("Simulated transient failure...");
            return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.TransientFailure };
        }     

        var client = httpClientFactory.CreateClient();
        var result = await client.GetAsync(serviceUrl);
        return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.CompletedWithoutResult };
    }
}