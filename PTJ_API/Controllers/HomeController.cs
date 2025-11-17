using Microsoft.AspNetCore.Mvc;
using PTJ_Service.HomeService;

namespace PTJ_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        [HttpGet("posts")]
        public async Task<IActionResult> GetHomePosts([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var posts = await _homeService.GetLatestPostsAsync(keyword, page, pageSize);
            return Ok(posts);
        }
        [HttpGet("statistics")]
        public async Task<IActionResult> GetHomeStatistics()
        {
            var stats = await _homeService.GetHomeStatisticsAsync();
            return Ok(stats);
        }
    }
}
