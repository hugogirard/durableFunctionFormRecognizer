//
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Seeder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeder
{
    public class SeederOrchestrator
    {
        
        [FunctionName("SeederOrchestrator")]
        public async Task<UploadResult> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var result = new UploadResult();

            try
            {
                result.TotalDocumentProcess = context.GetInput<OrchestratorParameter>().NbrDocuments;
                
                var nbrDocuments = context.GetInput<OrchestratorParameter>().NbrDocuments;

                int factor = Environment.GetEnvironmentVariable("FACTOR") == null
                             ? 10
                             : int.Parse(Environment.GetEnvironmentVariable("FACTOR"));

                int left = 0;
                int index = 0;

                if (nbrDocuments > factor)
                {
                    left = nbrDocuments % factor;
                    index = nbrDocuments / factor;
                }
                else
                {
                    index = 1;
                    factor = nbrDocuments;
                }

                for (int i = 0; i < index; i++)
                {
                    var tasks = StartActivies(factor, context);
                    await Task.WhenAll(tasks);
                    Aggregate(tasks.Select(t => t.Result), result);
                }

                if (left > 0)
                {
                    var tasks = StartActivies(left, context);
                    await Task.WhenAll(tasks);
                    Aggregate(tasks.Select(t => t.Result), result);
                }

                return result;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                throw;
            }


        }

        private Task<ActivityResult>[] StartActivies(int factor, IDurableOrchestrationContext context)
        {
            var tasks = new Task<ActivityResult>[factor];

            for (int i = 0; i < factor; i++)
            {
                string filename = $"{context.NewGuid()}.pdf";
                tasks[i] = context.CallActivityAsync<ActivityResult>("UploadInvoice", new  ActivityParameter()
                { 
                    Filename = filename,
                    ModelName = "ceo.pdf"
                });
            }

            return tasks;
        }

        private void Aggregate(IEnumerable<ActivityResult> results, UploadResult uploadResult)
        {
            uploadResult.TotalSuccessDocument += results.Count(r => r.IsSucces);
            uploadResult.TotalFailureDocument += results.Count(r => !r.IsSucces);
        }
    }
}
