using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/jobposts")]
    [Authorize(Roles = "Admin")]
    public class AdminJobPostController : ControllerBase
    {
        private readonly IAdminJobPostService _svc;
        public AdminJobPostController(IAdminJobPostService svc) => _svc = svc;

        // Employer Posts
        [HttpGet("employer")]
        public async Task<IActionResult> GetEmployerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
            => Ok(await _svc.GetEmployerPostsAsync(status, categoryId, keyword, page, pageSize));

        [HttpGet("employer/{id:int}")]
        public async Task<IActionResult> GetEmployerPostDetail(int id)
        {
            var data = await _svc.GetEmployerPostDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost("employer/{id:int}/toggle-block")]
        public async Task<IActionResult> ToggleEmployerPostBlocked(int id)
        {
            await _svc.ToggleEmployerPostBlockedAsync(id);
            return Ok(new { message = "Employer post status toggled." });
        }

        // JobSeeker Posts
        [HttpGet("jobseeker")]
        public async Task<IActionResult> GetJobSeekerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
            => Ok(await _svc.GetJobSeekerPostsAsync(status, categoryId, keyword, page, pageSize));

        [HttpGet("jobseeker/{id:int}")]
        public async Task<IActionResult> GetJobSeekerPostDetail(int id)
        {
            var data = await _svc.GetJobSeekerPostDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        [HttpPost("jobseeker/{id:int}/toggle-archive")]
        public async Task<IActionResult> ToggleJobSeekerPostArchived(int id)
        {
            await _svc.ToggleJobSeekerPostArchivedAsync(id);
            return Ok(new { message = "JobSeeker post status toggled." });
        }
    }
}
