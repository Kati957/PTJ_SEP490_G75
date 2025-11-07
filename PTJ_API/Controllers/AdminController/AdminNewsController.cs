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

        // List + filter
        [HttpGet]
        public async Task<IActionResult> GetAllNews(
            [FromQuery] string? status = null,
            [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetAllNewsAsync(status, keyword);
            return Ok(data);
        }

        // Detail
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNewsDetail([FromRoute] int id)
        {
            var data = await _svc.GetNewsDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        // Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminCreateNewsDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var newId = await _svc.CreateAsync(adminId, dto);
            return CreatedAtAction(nameof(GetNewsDetail), new { id = newId }, new { id = newId });
        }

        // Update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AdminUpdateNewsDto dto)
        {
            await _svc.UpdateAsync(id, dto);
            return Ok(new { message = "News updated successfully." });
        }

        // Toggle Active (Active <-> Hidden)
        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive([FromRoute] int id)
        {
            await _svc.ToggleActiveAsync(id);
            return Ok(new { message = "News status toggled." });
        }
    }
}
