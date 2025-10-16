using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace PTJ_Service.AIService
{
    public class PineconeService
    {
        private readonly HttpClient _http;

        public PineconeService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _http.BaseAddress = new Uri(cfg["Pinecone:IndexEndpoint"]!);
            _http.DefaultRequestHeaders.Add("Api-Key", cfg["Pinecone:ApiKey"]);
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task UpsertAsync(string ns, string id, float[] vector, object metadata)
        {
            var payload = new
            {
                vectors = new[] { new { id, values = vector, metadata } },
                @namespace = ns
            };
            var res = await _http.PostAsJsonAsync("/vectors/upsert", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<(string Id, double Score)>> QueryAsync(string ns, float[] vector, int topK = 5)
        {
            var payload = new
            {
                vector,
                topK,
                includeMetadata = true,
                @namespace = ns
            };
            var res = await _http.PostAsJsonAsync("/query", payload);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var matches = json.GetProperty("matches");
            var list = new List<(string, double)>();

            foreach (var m in matches.EnumerateArray())
            {
                list.Add((m.GetProperty("id").GetString()!, m.GetProperty("score").GetDouble()));
            }
            return list;
        }
    }
}
