using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace PTJ_Service.AIService
{
    public class AIService : IAIService
    {
        private readonly HttpClient _http;
        private readonly string _openAiKey;
        private readonly string _pineconeKey;
        private readonly string _pineconeUrl;

        public AIService(IConfiguration cfg)
        {
            _http = new HttpClient();

            _openAiKey = cfg["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI:ApiKey in appsettings.json");
            _pineconeKey = cfg["Pinecone:ApiKey"] ?? throw new Exception("Missing Pinecone:ApiKey in appsettings.json");
            _pineconeUrl = cfg["Pinecone:IndexEndpoint"] ?? throw new Exception("Missing Pinecone:IndexEndpoint in appsettings.json");

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        }

        // ===========================================
        // 🧠 GỌI OPENAI TẠO EMBEDDING VECTOR
        // ===========================================
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new
            {
                model = "text-embedding-3-large",
                input = text
            };

            var response = await _http.PostAsync(
                "https://api.openai.com/v1/embeddings",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json)!;

            var embedding = ((IEnumerable<dynamic>)result.data[0].embedding)
                .Select(x => (float)x)
                .ToArray();

            return embedding;
        }

        // ===========================================
        // 📦 GỬI VECTOR LÊN PINECONE
        // ===========================================
        public async Task UpsertVectorAsync(string ns, string id, float[] vector, object metadata)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
            {
                vectors = new[]
                {
                    new {
                        id,
                        values = vector,
                        metadata
                    }
                },
                @namespace = ns
            };

            var response = await client.PostAsJsonAsync($"{_pineconeUrl}/vectors/upsert", payload);
            response.EnsureSuccessStatusCode();
        }

        // ===========================================
        // 🔍 TRUY VẤN VECTOR TƯƠNG TỰ TRONG PINECONE
        // ===========================================
        public async Task<List<(string Id, double Score)>> QuerySimilarAsync(string ns, float[] vector, int topK)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
            {
                vector,
                topK,
                @namespace = ns,
                includeMetadata = true
            };

            var response = await client.PostAsJsonAsync($"{_pineconeUrl}/query", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json)!;

            var matches = new List<(string Id, double Score)>();
            foreach (var match in result.matches)
            {
                matches.Add(((string)match.id, (double)match.score));
            }

            return matches;
        }
    }
}
