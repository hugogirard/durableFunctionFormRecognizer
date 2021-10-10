using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICosmosService
{
    public Task SaveDocuments(IEnumerable<Document> documents);
}