using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.CategoryService.Interfaces;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/subcategory")]
    public class SubCategoryController : ControllerBase
        {
        private readonly ISubCategoryService _service;

        public SubCategoryController(ISubCategoryService service)
            {
            _service = service;
            }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            {
            return Ok(await _service.GetAllAsync());
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
            {
            var sub = await _service.GetByIdAsync(id);
            return sub == null ? NotFound() : Ok(sub);
            }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
            {
            return Ok(await _service.GetByCategoryIdAsync(categoryId));
            }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter(SubCategoryDTO.SubCategoryFilterDto dto)
            {
            return Ok(await _service.FilterAsync(dto));
            }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategoryDTO.SubCategoryCreateDto dto)
            {
            var created = await _service.CreateAsync(dto);
            return Ok(created);
            }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SubCategoryDTO.SubCategoryUpdateDto dto)
            {
            bool ok = await _service.UpdateAsync(id, dto);
            return ok ? Ok() : NotFound();
            }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            bool ok = await _service.DeleteAsync(id);
            return ok ? Ok() : NotFound();
            }
        }
    }
