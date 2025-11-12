using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.News;
using PTJ_Service.NewsService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/news")]
    [AllowAnonymous]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _svc;
        public NewsController(INewsService svc) => _svc = svc;

        // Danh sách public
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] bool desc = true)
        {
            var (data, total) = await _svc.GetPagedAsync(keyword, category, page, pageSize, sortBy, desc);
            return Ok(new { total, data });
        }

        // Chi tiết bài public
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail([FromRoute] int id)
        {
            var data = await _svc.GetDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }
    }
}
