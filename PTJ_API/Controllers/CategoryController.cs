using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.CategoryService.Interfaces;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/category")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        // GET ALL
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            bool isAdmin = User.IsInRole("Admin");

            var categories = await _service.GetCategoriesAsync(isAdmin);

            var result = categories.Select(c => new CategoryDTO.CategoryReadDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                CategoryGroup = c.CategoryGroup,
                Description = c.Description,
                IsActive = c.IsActive
            });

            return Ok(result);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            bool isAdmin = User.IsInRole("Admin");
            var c = await _service.GetByIdAsync(id, isAdmin);

            if (c == null)
                return NotFound();

            var result = new CategoryDTO.CategoryReadDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                CategoryGroup = c.CategoryGroup,
                Description = c.Description,
                IsActive = c.IsActive
            };

            return Ok(result);
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create(CategoryDTO.CategoryCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);

            if (created == null)
                return BadRequest("Category name already exists.");

            var result = new CategoryDTO.CategoryReadDto
            {
                CategoryId = created.CategoryId,
                Name = created.Name,
                CategoryGroup = created.CategoryGroup,
                Description = created.Description,
                IsActive = created.IsActive
            };

            return Ok(result);
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryDTO.CategoryUpdateDto dto)
        {
            bool success = await _service.UpdateAsync(id, dto);

            return success
                ? Ok("Updated successfully.")
                : BadRequest("Category not found or name already exists.");
        }

        // SOFT DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _service.DeleteAsync(id);

            return success ? Ok("Deleted successfully.") : NotFound();
        }

        // FILTER
        [HttpPost("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> Filter(CategoryDTO.CategoryFilterDto dto)
        {
            bool isAdmin = User.IsInRole("Admin");

            var categories = await _service.FilterAsync(dto, isAdmin);

            var result = categories.Select(c => new CategoryDTO.CategoryReadDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                CategoryGroup = c.CategoryGroup,
                Description = c.Description,
                IsActive = c.IsActive
            });

            return Ok(result);
        }
    }
}
