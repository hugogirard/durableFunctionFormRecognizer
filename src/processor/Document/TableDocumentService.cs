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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Newtonsoft.Json;

public class TableDocumentService : IDocumentService
{
    private TableClient tableClient;

    public TableDocumentService(TableClient tableClient)
    {        
        this.tableClient = tableClient;
    }

    public async Task SaveDocuments(IEnumerable<Document> documents)
    {
        var tasks = new List<Task>();
        foreach(var document in documents)
        {
            var tableEntity = new TableEntity("1", document.Id);
            tableEntity.Add("State", document.State);
            tableEntity.Add("Exception", document.Exception);
            tableEntity.Add("TransientFailureCount", document.TransientFailureCount);
            tableEntity.Add("Forms", JsonConvert.SerializeObject(document.Forms));
            tasks.Add(tableClient.UpsertEntityAsync<TableEntity>(tableEntity, TableUpdateMode.Merge, CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }
}