using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.News;
using PTJ_Service.NewsService;

namespace PTJ_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] NewsCreateDto dto)
        {
            try
            {
                var news = await _newsService.CreateAsync(dto);
                return Ok(new
                {
                    message = "Tạo bài viết thành công!",
                    data = news
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] bool desc = true)
        {
            var (data, total) = await _newsService.GetPagedAsync(keyword, category, page, pageSize, sortBy, desc);
            return Ok(new
            {
                total,
                data
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _newsService.GetPagedAsync(null, null, 1, 1, "CreatedAt", true);
            var newsItem = result.Data.FirstOrDefault(n => n.NewsID == id);

            if (newsItem == null)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            return Ok(newsItem);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] NewsUpdateDto dto)
        {
            try
            {
                dto.NewsID = id;
                var updated = await _newsService.UpdateAsync(dto);
                if (updated == null)
                    return NotFound(new { message = "Không tìm thấy bài viết để cập nhật." });

                return Ok(new
                {
                    message = "Cập nhật bài viết thành công!",
                    data = updated
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _newsService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Không tìm thấy bài viết để xóa." });

            return Ok(new { message = "Xóa bài viết thành công!" });
        }
    }
}
