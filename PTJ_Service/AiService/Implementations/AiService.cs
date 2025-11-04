using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PTJ_Data;
using PTJ_Models;
using PTJ_Service.AiService.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace PTJ_Service.AiService.Implementations
{
    public class AIService : IAIService
    {
        private readonly HttpClient _http;
        private readonly JobMatchingDbContext _db;
        private readonly string _openAiKey;
        private readonly string _pineconeKey;
        private readonly string _pineconeUrl;

        public AIService(IConfiguration cfg, JobMatchingDbContext db)
        {
            _db = db;
            _http = new HttpClient();

            //_openAiKey = cfg["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI:ApiKey in appsettings.json");
            //_pineconeKey = cfg["Pinecone:ApiKey"] ?? throw new Exception("Missing Pinecone:ApiKey in appsettings.json");
            //_pineconeUrl = cfg["Pinecone:IndexEndpoint"] ?? throw new Exception("Missing Pinecone:IndexEndpoint in appsettings.json");

            //_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        }

        // =====================================================
        // 🧠 Create Embedding
        // =====================================================
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new { model = "text-embedding-3-large", input = text };

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

        // =====================================================
        // 📤 Upsert Vector vào Pinecone
        // =====================================================
        public async Task UpsertVectorAsync(string ns, string id, float[] vector, object metadata)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var metaDict = metadata
                .GetType()
                .GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(metadata, null) ?? ""
                );

            var payload = new
            {
                vectors = new[]
                {
                    new {
                        id,
                        values = vector,
                        metadata = metaDict
                    }
                },
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
            };

            var res = await client.PostAsJsonAsync($"{_pineconeUrl}/vectors/upsert", payload);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone Upsert failed: {res.StatusCode} - {body}");
            }

            // ⏳ Đợi Pinecone index xong để query liền sau
            await Task.Delay(1500);
        }

        // =====================================================
        // 🔍 Query Similar — hỗ trợ Pinecone v1/v2 + riêng namespace
        // =====================================================
        public async Task<List<(string Id, double Score)>> QuerySimilarAsync(string ns, float[] vector, int topK)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
            {
                vector,
                topK,
                includeMetadata = true,
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
            };

            var res = await client.PostAsJsonAsync($"{_pineconeUrl}/query", payload);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json)!;

            var list = new List<(string Id, double Score)>();

            // ✅ Pinecone v1
            if (result?["matches"] != null)
            {
                foreach (var m in result.matches)
                    list.Add(((string)m.id, (double)m.score));
            }
            // ✅ Pinecone v2
            else if (result?["results"] != null)
            {
                foreach (var resObj in result.results)
                {
                    if (resObj?["matches"] != null)
                    {
                        foreach (var m in resObj.matches)
                            list.Add(((string)m.id, (double)m.score));
                    }
                }
            }

            return list;
        }
    }
}
