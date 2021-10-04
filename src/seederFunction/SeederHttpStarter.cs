using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Seeder.Model;

namespace Seeder
{
    public class SeederHttpStarter
    {
        [FunctionName("SeederHttpStarter")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {

            string queryString = req.Query["nbrDocument"];

            if (string.IsNullOrEmpty(queryString))
            {
                return new BadRequestObjectResult("Please pass the nbrDocument in the query string");
            }

            int nbrDocuments;
            if (!int.TryParse(queryString, out nbrDocuments)) 
            {
                return new BadRequestObjectResult("The parameter nbrDocuments need to be numeric");
            }

            string instanceId = await starter.StartNewAsync("SeederOrchestrator", new OrchestratorParameter(nbrDocuments));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
