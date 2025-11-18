using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.SearchService.Interfaces;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
        {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
            {
            _service = service;
            }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            {
            var categories = await _service.GetCategoriesAsync();
            return Ok(categories);
            }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
            {
            var category = await _service.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
            }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CategoryDTO.Create dto)
            {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, created);
            }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDTO.Update dto)
            {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();
            return Ok(new { message = "Cập nhật danh mục thành công." });
            }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
            {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(new { message = "Xóa danh mục thành công." });
            }
        }
    }
