using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_API.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin/news")]
    [Authorize(Roles = "Admin")]
    public class AdminNewsController : ControllerBase
    {
        private readonly IAdminNewsService _svc;
        public AdminNewsController(IAdminNewsService svc) => _svc = svc;

        //  Danh sách + lọc
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isPublished = null,
            [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetAllNewsAsync(isPublished, keyword);
            return Ok(data);
        }

        //  Chi tiết
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail([FromRoute] int id)
        {
            var data = await _svc.GetNewsDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
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
        public async Task<IActionResult> Update([FromRoute] int id, [FromForm] AdminUpdateNewsDto dto)
        {
            dto.NewsId = id;
            await _svc.UpdateAsync(dto);
            return Ok(new { message = "News updated successfully." });
        }

        //  Publish / Unpublish
        [HttpPost("{id:int}/toggle-publish")]
        public async Task<IActionResult> TogglePublish([FromRoute] int id)
        {
            await _svc.TogglePublishAsync(id);
            return Ok(new { message = "Publish status changed successfully." });
        }

        //  Xóa mềm
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            await _svc.DeleteAsync(id);
            return Ok(new { message = "News deleted successfully." });
        }
    }
}
