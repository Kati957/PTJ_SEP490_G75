using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.CategoryService.Interfaces;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/category")]
    public class CategoryController : ControllerBase
        {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
            {
            _service = service;
            }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            {
            return Ok(await _service.GetCategoriesAsync());
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
            {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
            }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryDTO.CategoryCreateDto dto)
            {
            var created = await _service.CreateAsync(dto);
            return Ok(created);
            }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryDTO.CategoryUpdateDto dto)
            {
            bool success = await _service.UpdateAsync(id, dto);
            return success ? Ok() : NotFound();
            }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            bool success = await _service.DeleteAsync(id);
            return success ? Ok() : NotFound();
            }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter(CategoryDTO.CategoryFilterDto dto)
            {
            return Ok(await _service.FilterAsync(dto));
            }
        }
    }
