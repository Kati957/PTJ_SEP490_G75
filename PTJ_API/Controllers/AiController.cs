using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
        {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public AiController(IConfiguration config)
            {
            _config = config;
            _http = new HttpClient();
            }

        // ==============================
        // 🧩 FULL AI TEST: EMBED + UPSERT + QUERY
        // ==============================
        [HttpPost("test-full")]
        public async Task<IActionResult> TestFull([FromBody] string text)
            {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "Missing input text" });

            // 1️⃣ Lấy cấu hình OpenAI
            var openAiKey = _config["OpenAI:ApiKey"];
            var openAiModel = _config["OpenAI:Model"] ?? "text-embedding-3-large";
            if (string.IsNullOrEmpty(openAiKey))
                return StatusCode(500, new { error = "Missing OpenAI API key" });

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiKey);

            // 2️⃣ Gọi OpenAI để tạo embedding
            var embedRequest = new
                {
                model = openAiModel,
                input = text
                };

            var embedRes = await _http.PostAsJsonAsync("https://api.openai.com/v1/embeddings", embedRequest);
            embedRes.EnsureSuccessStatusCode();

            var embedJson = await embedRes.Content.ReadAsStringAsync();
            var embedResult = JsonDocument.Parse(embedJson);
            var vector = embedResult.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();

            // 3️⃣ Gửi vector lên Pinecone (Upsert)
            var pineconeKey = _config["Pinecone:ApiKey"];
            var pineconeEndpoint = _config["Pinecone:IndexEndpoint"];
            if (string.IsNullOrEmpty(pineconeKey) || string.IsNullOrEmpty(pineconeEndpoint))
                return StatusCode(500, new { error = "Missing Pinecone configuration" });

            using var pinecone = new HttpClient();
            pinecone.BaseAddress = new Uri(pineconeEndpoint);
            pinecone.DefaultRequestHeaders.Add("Api-Key", pineconeKey);
            pinecone.DefaultRequestHeaders.Add("Accept", "application/json");

            string vectorId = $"TestVec-{Guid.NewGuid()}";
            string ns = "test_namespace";

            var upsertPayload = new
                {
                vectors = new[]
                {
                    new { id = vectorId, values = vector, metadata = new { text } }
                },
                @namespace = ns
                };

            var upsertRes = await pinecone.PostAsJsonAsync("/vectors/upsert", upsertPayload);
            upsertRes.EnsureSuccessStatusCode();

            // 4️⃣ Query lại Pinecone để so sánh độ tương đồng
            var queryPayload = new
                {
                vector = vector,
                topK = 3,
                includeMetadata = true,
                @namespace = ns
                };

            var queryRes = await pinecone.PostAsJsonAsync("/query", queryPayload);
            queryRes.EnsureSuccessStatusCode();

            var queryJson = await queryRes.Content.ReadAsStringAsync();
            var queryResult = JsonDocument.Parse(queryJson);

            var matches = queryResult.RootElement.GetProperty("matches")
                .EnumerateArray()
                .Select(m => new
                    {
                    Id = m.GetProperty("id").GetString(),
                    Score = Math.Round(m.GetProperty("score").GetDouble() * 100, 2)
                    })
                .ToList();

            // 5️⃣ Trả về kết quả
            return Ok(new
                {
                message = "✅ AI pipeline successful: OpenAI → Pinecone → Compare",
                openAiModel,
                vectorLength = vector.Length,
                savedAs = vectorId,
                matchResults = matches
                });
            }
        }
    }
