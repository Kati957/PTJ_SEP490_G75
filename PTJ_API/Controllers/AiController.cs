using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.AIService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly AiMatchService _ai;

        public AiController(AiMatchService ai)
        {
            _ai = ai;
        }

        [HttpPost("embed")]
        public async Task<IActionResult> Embed([FromBody] PostDto post)
        {
            await _ai.SavePostEmbeddingAsync(post.Type, post.Id, post.Title, post.Description, post.Location);
            return Ok(new { message = "Đã lưu embedding vào Pinecone" });
        }

        [HttpGet("recommend")]
        public async Task<IActionResult> Recommend([FromQuery] string type, [FromQuery] string query)
        {
            var results = await _ai.FindSimilarAsync(
                type == "Employer" ? "employer_posts" : "job_seeker_posts",
                query
            );
            return Ok(results);
        }
    }
}
