using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace PTJ_Service.AIService
{
    public class OpenAIService
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public OpenAIService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://api.openai.com/v1/");
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg["OpenAI:ApiKey"]);
            _model = cfg["OpenAI:EmbeddingsModel"]!;
        }

        private record EmbeddingResponse(List<EmbeddingData> data);
        private record EmbeddingData(List<float> embedding);

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new { model = _model, input = text };
            var response = await _http.PostAsJsonAsync("embeddings", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            return result!.data[0].embedding.ToArray();
        }
    }
}
