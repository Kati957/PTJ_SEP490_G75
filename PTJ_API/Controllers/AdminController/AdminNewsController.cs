using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Admin.Interfaces;
using PTJ_Models.DTO.Admin;
using System.Security.Claims;

namespace PTJ_API.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin/news")]
    [Authorize(Roles = "Admin")]
    public class AdminNewsController : ControllerBase
    {
        private readonly IAdminNewsService _svc;

        public AdminNewsController(IAdminNewsService svc)
        {
            _svc = svc;
        }

        //  Danh sách + lọc
        [HttpGet]
        public async Task<IActionResult> GetAllNews([FromQuery] bool? isPublished = null, [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetAllNewsAsync(isPublished, keyword);
            return Ok(data);
        }

        //  Chi tiết
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var news = await _svc.GetNewsDetailAsync(id);
            return news is null ? NotFound() : Ok(news);
        }

        //  Tạo mới
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AdminCreateNewsDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var id = await _svc.CreateAsync(adminId, dto);
            return CreatedAtAction(nameof(GetDetail), new { id }, new { id });
        }

        //  Cập nhật
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromForm] AdminUpdateNewsDto dto)
        {
            dto.NewsId = id;
            await _svc.UpdateAsync(dto);
            return Ok(new { message = "News updated successfully." });
        }

        // Publish / Unpublish
        [HttpPost("{id:int}/toggle-publish")]
        public async Task<IActionResult> TogglePublish(int id)
        {
            await _svc.TogglePublishStatusAsync(id);
            return Ok(new { message = "Publish status changed successfully." });
        }

        //  Xóa mềm
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _svc.DeleteAsync(id);
            return Ok(new { message = "News deleted successfully." });
        }
    }
}
