using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class CosmosService : ICosmosService
{
    private Container container;

    public CosmosService(Container container)
    {
        this.container = container;
    }

    public async Task SaveDocuments(IEnumerable<Document> documents)
    {
        var tasks = new List<Task>();        
        foreach(var document in documents)
        {
            tasks.Add(container.UpsertItemAsync<Document>(document, new PartitionKey(document.Id)));
        }
        await Task.WhenAll(tasks);
    }
}