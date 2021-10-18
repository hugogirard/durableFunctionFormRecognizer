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
            throw new TransientFailureException();
        }      

        if (serviceUrl != null)
        {
            var client = httpClientFactory.CreateClient();
            var result = await client.GetAsync(serviceUrl);
        }
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
            throw new TransientFailureException();
        }     

        if (serviceUrl != null)
        {
            var client = httpClientFactory.CreateClient();
            var result = await client.GetAsync(serviceUrl);
        }
        return new FormRecognizerResult() { Status = FormRecognizerResult.ResultStatus.CompletedWithoutResult };
    }
}