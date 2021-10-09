using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CosmosMockService : ICosmosService
{
    public Task SaveDocuments(IEnumerable<Document> documents)
    {        
        return Task.CompletedTask;     
    }
}