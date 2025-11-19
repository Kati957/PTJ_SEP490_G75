using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_API.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "Admin")]
    public class AdminCategoryController : ControllerBase
    {
        private readonly IAdminCategoryService _svc;
        public AdminCategoryController(IAdminCategoryService svc) => _svc = svc;

        // List + filter
        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] string? type = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetCategoriesAsync(type, isActive, keyword);
            return Ok(data);
        }

        // Detail
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory([FromRoute] int id)
        {
            var data = await _svc.GetCategoryAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        // Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminCreateCategoryDto dto)
        {
            var newId = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetCategory), new { id = newId }, new { id = newId });
        }

        // Update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AdminUpdateCategoryDto dto)
        {
            await _svc.UpdateAsync(id, dto);
            return Ok(new { message = "Cập nhật danh mục thành công." });
        }

        // Toggle Active
        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive([FromRoute] int id)
        {
            await _svc.ToggleActiveAsync(id);
            return Ok(new { message = "Đã cập nhật trạng thái hoạt động của danh mục." });
        }
    }
}
