using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CategoryDTO;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/subcategory")]
    [Authorize(Roles = "Admin")]
    public class SubCategoryController : ControllerBase
        {
        private readonly ISubCategoryService _service;

        public SubCategoryController(ISubCategoryService service)
            {
            _service = service;
            }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            {
            bool isAdmin = User.IsInRole("Admin");
            var result = await _service.GetAllAsync(isAdmin);
            return Ok(result);
            }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
            {
            bool isAdmin = User.IsInRole("Admin");
            var sub = await _service.GetByIdAsync(id, isAdmin);
            return sub == null ? NotFound() : Ok(sub);
            }

        [HttpGet("by-category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategory(int categoryId)
            {
            bool isAdmin = User.IsInRole("Admin");
            var result = await _service.GetByCategoryIdAsync(categoryId, isAdmin);
            return Ok(result);
            }

        [HttpPost("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> Filter(SubCategoryDTO.SubCategoryFilterDto dto)
            {
            bool isAdmin = User.IsInRole("Admin");
            var result = await _service.FilterAsync(dto, isAdmin);
            return Ok(result);
            }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategoryDTO.SubCategoryCreateDto dto)
            {
            var created = await _service.CreateAsync(dto);
            if (created == null)
                return BadRequest("SubCategory name already exists in this Category.");
            return Ok(created);
            }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SubCategoryDTO.SubCategoryUpdateDto dto)
            {
            bool ok = await _service.UpdateAsync(id, dto);
            return ok ? Ok() : BadRequest("SubCategory not found or name already exists.");
            }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            bool ok = await _service.DeleteAsync(id);
            return ok ? Ok() : NotFound();
            }
        }
    }
