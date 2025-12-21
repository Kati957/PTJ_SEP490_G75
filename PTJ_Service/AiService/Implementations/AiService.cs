using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Service.AiService;

namespace PTJ_Service.AiService.Implementations
    {
    public class AIService : IAIService
        {
        private readonly HttpClient _http;
        private readonly JobMatchingDbContext _db;
        private readonly string _pineconeKey;
        private readonly string _pineconeUrl;
        private readonly string _openAiKey;
        private readonly string _openAiUrl;
        private readonly string _embeddingModel;

        public AIService(IConfiguration cfg, JobMatchingDbContext db)
            {
            _db = db;
            _http = new HttpClient();

            _openAiKey = cfg["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI:ApiKey");
            _embeddingModel = cfg["OpenAI:EmbeddingsModel"] ?? "text-embedding-3-large";

            // luôn dùng endpoint mặc định
            _openAiUrl = "https://api.openai.com/v1";

            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiKey}");

            // Pinecone
            _pineconeKey = cfg["Pinecone:ApiKey"] ?? throw new Exception("Missing Pinecone:ApiKey");
            _pineconeUrl = cfg["Pinecone:IndexEndpoint"] ?? throw new Exception("Missing Pinecone:IndexEndpoint");
            }




        // =====================================================
        // 🧠 Create Embedding via LM Studio (local)
        // =====================================================
        public async Task<float[]> CreateEmbeddingAsync(string text)
            {
            var payload = new
                {
                model = _embeddingModel,
                input = text
                };

            var response = await _http.PostAsJsonAsync($"{_openAiUrl}/embeddings", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var dataArray = json.GetProperty("data");

            if (dataArray.GetArrayLength() == 0)
                throw new Exception("OpenAI trả về embedding rỗng.");

            var embedding = dataArray[0].GetProperty("embedding")
                .EnumerateArray()
                .Select(x => (float)x.GetDouble())
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
                vectors = new[] { new { id, values = vector, metadata = metaDict } },
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
                };

            var res = await client.PostAsJsonAsync($"{_pineconeUrl}/vectors/upsert", payload);
            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone Upsert failed: {res.StatusCode} - {body}");
                }
            }

        // =====================================================
        // 🔍 Query Similar from Pinecone
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

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var list = new List<(string Id, double Score)>();

            if (json.TryGetProperty("matches", out var matches))
                {
                foreach (var m in matches.EnumerateArray())
                    list.Add((m.GetProperty("id").GetString()!, m.GetProperty("score").GetDouble()));
                }

            return list;
            }
        }
    }
