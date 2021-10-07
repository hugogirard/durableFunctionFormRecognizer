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
                             ? 100
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

                if (Environment.GetEnvironmentVariable("Mode") == "parallel")
                {
                    for (int i = 0; i < index; i++)
                    {
                        var activityResult = await StartActiviesWithTasks(factor, context);
                        Aggregate(activityResult, result);
                    }

                    if (left > 0)
                    {
                        var activityResult = await StartActiviesWithTasks(left, context);
                        Aggregate(activityResult, result);
                    }
                }
                else 
                {
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
                }



                return result;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                throw;
            }


        }

        private async Task<IEnumerable<ActivityResult>> StartActiviesWithTasks(int factor, IDurableOrchestrationContext context)
        {
            var filenames = new List<string>();
            for (int i = 0; i < factor; i++)
            {
                filenames.Add($"{context.NewGuid()}.pdf");
            }

            return await context.CallActivityAsync<IEnumerable<ActivityResult>>("UploadInvoice", new  ActivityParameter()
            { 
                Filenames = filenames,
                ModelName = "ceo.pdf"
            });
        }

        private Task<ActivityResult>[] StartActivies(int factor, IDurableOrchestrationContext context)
        {
            var tasks = new Task<ActivityResult>[factor];

            for (int i = 0; i < factor; i++)
            {
                string filename = $"{context.NewGuid()}.pdf";
                tasks[i] = context.CallActivityAsync<ActivityResult>("UploadInvoice", new ActivityParameter()
                {
                    Filenames = new List<string> { filename },
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
