using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly HttpClient _openAiHttp;
        private readonly HttpClient _pineconeHttp;
        private readonly JobMatchingOpenAiDbContext _db;

        private readonly string _openAiUrl = "https://api.openai.com/v1";
        private readonly string _embeddingModel;
        private readonly string _pineconeUrl;

        public AIService(IConfiguration cfg, JobMatchingOpenAiDbContext db)
            {
            _db = db;

            // ===============================
            // OpenAI
            // ===============================
            var openAiKey = cfg["OpenAI:ApiKey"]
                ?? throw new Exception("Missing OpenAI:ApiKey");

            _embeddingModel = cfg["OpenAI:EmbeddingsModel"]
                ?? "text-embedding-3-large";

            _openAiHttp = new HttpClient();
            _openAiHttp.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", openAiKey);

            // ===============================
            // Pinecone
            // ===============================
            var pineconeKey = cfg["Pinecone:ApiKey"]
                ?? throw new Exception("Missing Pinecone:ApiKey");

            _pineconeUrl = cfg["Pinecone:IndexEndpoint"]
                ?? throw new Exception("Missing Pinecone:IndexEndpoint");

            _pineconeHttp = new HttpClient();
            _pineconeHttp.DefaultRequestHeaders.Add("Api-Key", pineconeKey);
            _pineconeHttp.DefaultRequestHeaders.Add("Accept", "application/json");
            }

        // =====================================================
        // 🧠 CREATE EMBEDDING (OPENAI)
        // =====================================================
        public async Task<float[]> CreateEmbeddingAsync(string text)
            {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            // giới hạn để tránh tốn token
            if (text.Length > 6000)
                text = text[..6000];

            var payload = new
                {
                model = _embeddingModel,
                input = text
                };

            var res = await _openAiHttp.PostAsJsonAsync(
                $"{_openAiUrl}/embeddings",
                payload
            );

            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI Embedding failed: {res.StatusCode} - {body}");
                }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");

            if (data.GetArrayLength() == 0)
                throw new Exception("OpenAI trả về embedding rỗng.");

            return data[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => (float)x.GetDouble())
                .ToArray();
            }

        // =====================================================
        // 📤 UPSERT VECTOR VÀO PINECONE
        // =====================================================
        public async Task UpsertVectorAsync(string ns, string id, float[] vector, object metadata)
            {
            var metaDict = metadata
                .GetType()
                .GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(metadata) ?? ""
                );

            var payload = new
                {
                vectors = new[]
                {
                    new
                    {
                        id,
                        values = vector,
                        metadata = metaDict
                    }
                },
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
                };

            var res = await _pineconeHttp.PostAsJsonAsync(
                $"{_pineconeUrl}/vectors/upsert",
                payload
            );

            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone Upsert failed: {res.StatusCode} - {body}");
                }
            }

        //// =====================================================
        //// 🔍 QUERY SIMILAR (KHÔNG FILTER)
        //// =====================================================
        //public async Task<List<(string Id, double Score)>> QuerySimilarAsync(
        //    string ns,
        //    float[] vector,
        //    int topK)
        //    {
        //    var payload = new
        //        {
        //        vector,
        //        topK,
        //        includeMetadata = true,
        //        @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
        //        };

        //    var res = await _pineconeHttp.PostAsJsonAsync(
        //        $"{_pineconeUrl}/query",
        //        payload
        //    );

        //    res.EnsureSuccessStatusCode();

        //    var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        //    var list = new List<(string, double)>();

        //    if (json.TryGetProperty("matches", out var matches))
        //        {
        //        foreach (var m in matches.EnumerateArray())
        //            {
        //            list.Add((
        //                m.GetProperty("id").GetString()!,
        //                m.GetProperty("score").GetDouble()
        //            ));
        //            }
        //        }

        //    return list;
        //    }

        // =====================================================
        // 🔍 QUERY SIMILAR (FILTER THEO ID) – GIỮ NGUYÊN LOGIC
        // =====================================================
        public async Task<List<(string Id, double Score)>> QueryWithIDsAsync(
            string ns,
            float[] vector,
            IEnumerable<int> allowedIds,
            int topK = 50)
            {
            var filter = new Dictionary<string, object>
                {
                ["numericPostId"] = new Dictionary<string, object>
                    {
                    ["$in"] = allowedIds.ToArray()
                    }
                };

            var payload = new
                {
                vector,
                topK,
                includeMetadata = true,
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns,
                filter
                };

            var res = await _pineconeHttp.PostAsJsonAsync(
                $"{_pineconeUrl}/query",
                payload
            );

            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone QUERY failed: {res.StatusCode} - {body}");
                }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var list = new List<(string, double)>();

            if (json.TryGetProperty("matches", out var matches))
                {
                foreach (var m in matches.EnumerateArray())
                    {
                    var score = m.GetProperty("score").GetDouble();
                    if (score >= 0.6) // giữ đúng logic cũ của bạn
                        {
                        list.Add((
                            m.GetProperty("id").GetString()!,
                            score
                        ));
                        }
                    }
                }

            return list
                .OrderByDescending(x => x.Item2)
                .ToList();
            }

        // =====================================================
        // 🗑️ DELETE VECTOR
        // =====================================================
        public async Task DeleteVectorAsync(string ns, string id)
            {
            var payload = new
                {
                ids = new[] { id },
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
                };

            var res = await _pineconeHttp.PostAsJsonAsync(
                $"{_pineconeUrl}/vectors/delete",
                payload
            );

            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone DELETE failed: {res.StatusCode} - {body}");
                }
            }
        }
    }
