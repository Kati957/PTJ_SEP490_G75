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
        private readonly string _lmStudioUrl;
        private readonly string _embeddingModel;

        public AIService(IConfiguration cfg, JobMatchingDbContext db)
            {
            _db = db;
            _http = new HttpClient();

            //  Pinecone config 
            _pineconeKey = cfg["Pinecone:ApiKey"] ?? throw new Exception("Missing Pinecone:ApiKey");
            _pineconeUrl = cfg["Pinecone:IndexEndpoint"] ?? throw new Exception("Missing Pinecone:IndexEndpoint");

            //  LM Studio config 
            //  Ví dụ: http://127.0.0.1:1234/v1
            _lmStudioUrl = cfg["LMStudio:Url"] ?? "http://127.0.0.1:1234";

            _embeddingModel = cfg["LMStudio:EmbeddingModel"] ?? "text-embedding-nomic-embed-text-v2-moe";
            }


        // Create Embedding via LM Studio (local)

        public async Task<float[]> CreateEmbeddingAsync(string text)
            {
            await CheckLMStudioHealthAsync();

            var payload = new
                {
                model = _embeddingModel,
                input = text
                };

            //  Đúng endpoint OpenAI-style
            var response = await _http.PostAsJsonAsync($"{_lmStudioUrl}/embeddings", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            //  LM Studio trả về data[0].embedding
            var dataArray = json.GetProperty("data");
            if (dataArray.GetArrayLength() == 0)
                throw new Exception("LM Studio trả về mảng embedding rỗng.");

            var embedding = dataArray[0].GetProperty("embedding")
                .EnumerateArray()
                .Select(x => (float)x.GetDouble())
                .ToArray();

            return embedding;
            }


        //  Upsert Vector vào Pinecone

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


        //  Query Similar from Pinecone

        //public async Task<List<(string Id, double Score)>> QuerySimilarAsync(string ns, float[] vector, int topK)
        //    {
        //    using var client = new HttpClient();
        //    client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
        //    client.DefaultRequestHeaders.Add("Accept", "application/json");

        //    var payload = new
        //        {
        //        vector,
        //        topK,
        //        includeMetadata = true,
        //        @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
        //        };

        //    var res = await client.PostAsJsonAsync($"{_pineconeUrl}/query", payload);
        //    res.EnsureSuccessStatusCode();

        //    var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        //    var list = new List<(string Id, double Score)>();

        //    if (json.TryGetProperty("matches", out var matches))
        //        {
        //        foreach (var m in matches.EnumerateArray())
        //            list.Add((m.GetProperty("id").GetString()!, m.GetProperty("score").GetDouble()));
        //        }

        //    return list;
        //    }

        //so sánh với những ứng viên bạn đã lọc theo Category + Location trong SQL
        public async Task<List<(string Id, double Score)>> QueryWithIDsAsync(
     string ns,
     float[] vector,
     IEnumerable<int> allowedIds,
     int topK = 50)
            {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // Ép về int[] cho chắc
            var idArray = allowedIds.ToArray();

            // Xây filter đúng cú pháp: { "numericPostId": { "$in": [1,2,3] } }
            var filter = new Dictionary<string, object>
                {
                ["numericPostId"] = new Dictionary<string, object>
                    {
                    ["$in"] = idArray    // ✅ đúng: $in
                    }
                };

            var payload = new
                {
                vector,
                topK,
                includeMetadata = true,
                @namespace = ns,
                filter
                };

            var res = await client.PostAsJsonAsync($"{_pineconeUrl}/query", payload);
            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone QUERY failed: {res.StatusCode} - {body}");
                }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var list = new List<(string Id, double Score)>();

            if (json.TryGetProperty("matches", out var matches))
                {
                foreach (var m in matches.EnumerateArray())
                    {
                    var id = m.GetProperty("id").GetString();
                    var score = m.GetProperty("score").GetDouble();

                    // ✅ chỉ lấy những vector có score >= 0.5
                    if (score >= 0.1)
                        {
                        list.Add((id!, score));
                        }
                    }
                }

            // Sắp xếp giảm dần theo score
            return list.OrderByDescending(x => x.Score).ToList();
            }

        //  Kiểm tra kết nối LM Studio

        //private async Task CheckLMStudioHealthAsync()
        //    {
        //    try
        //        {
        //        using var healthCheck = new HttpClient();
        //        //  LM Studio dùng endpoint OpenAI-style
        //        var res = await healthCheck.GetAsync($"{_lmStudioUrl}/models");
        //        if (!res.IsSuccessStatusCode)
        //            {
        //            throw new Exception("LM Studio local API không phản hồi. Hãy đảm bảo LM Studio đang mở và bật 'Local Inference Server'.");
        //            }
        //        }
        //    catch
        //        {
        //        throw new Exception("Không thể kết nối LM Studio. Vui lòng mở LM Studio và bật Local Server (Settings → Developer → Enable Local Inference Server).");
        //        }
        //    }

        private async Task CheckLMStudioHealthAsync()
            {
            try
                {
                using var healthCheck = new HttpClient();

                var payload = new
                    {
                    model = _embeddingModel,
                    input = "ping"
                    };

                var res = await healthCheck.PostAsJsonAsync($"{_lmStudioUrl}/embeddings", payload);

                if (!res.IsSuccessStatusCode)
                    throw new Exception("Embedding server không phản hồi.");
                }
            catch (Exception ex)
                {
                throw new Exception("Không thể kết nối Embedding Server (VPS).");
                }
            }

        public async Task DeleteVectorAsync(string ns, string id)
            {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Api-Key", _pineconeKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
                {
                ids = new[] { id },
                @namespace = string.IsNullOrWhiteSpace(ns) ? "default" : ns
                };

            var res = await client.PostAsJsonAsync($"{_pineconeUrl}/vectors/delete", payload);

            if (!res.IsSuccessStatusCode)
                {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Pinecone DELETE failed: {res.StatusCode} - {body}");
                }
            }
        }
    }
