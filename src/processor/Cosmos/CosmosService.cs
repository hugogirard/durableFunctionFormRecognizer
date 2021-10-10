using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class CosmosService : ICosmosService
{
    private int batchSize;
    private Container container;

    public CosmosService(Container container, int batchSize)
    {        
        this.container = container;
        this.batchSize = batchSize;
    }

    public async Task SaveDocuments(IEnumerable<Document> documents)
    {        
        foreach(var documentPartition in documents.Partition(batchSize))
        {
            var tasks = new List<Task>(); 
            foreach(var document in documentPartition)
            {
                tasks.Add(container.UpsertItemAsync<Document>(document, new PartitionKey(document.Id)));
            }
            await Task.WhenAll(tasks);
        }        
    }
}