using System.Net.Http;

namespace Viewer.Services
{
    public class ModelService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ModelService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        
    }
}
