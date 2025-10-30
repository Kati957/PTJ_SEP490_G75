using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Admin.Interfaces;
using PTJ_Service.Interface;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/jobposts")]
    [Authorize(Roles = "Admin")]
    public class AdminJobPostController : ControllerBase
    {
        private readonly IAdminJobPostService _svc;
        public AdminJobPostController(IAdminJobPostService svc) => _svc = svc;

        // ================= Employer Posts =================

        // List + filter: status, categoryId, keyword
        [HttpGet("employer")]
        public async Task<IActionResult> GetEmployerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetEmployerPostsAsync(status, categoryId, keyword);
            return Ok(data);
        }

        // Detail
        [HttpGet("employer/{id:int}")]
        public async Task<IActionResult> GetEmployerPostDetail([FromRoute] int id)
        {
            var data = await _svc.GetEmployerPostDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        // Toggle Block (Active <-> Blocked)
        [HttpPost("employer/{id:int}/toggle-block")]
        public async Task<IActionResult> ToggleEmployerPostBlocked([FromRoute] int id)
        {
            await _svc.ToggleEmployerPostBlockedAsync(id);
            return Ok(new { message = "Employer post block toggled." });
        }

        // ================= JobSeeker Posts =================

        // List + filter
        [HttpGet("jobseeker")]
        public async Task<IActionResult> GetJobSeekerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null)
        {
            var data = await _svc.GetJobSeekerPostsAsync(status, categoryId, keyword);
            return Ok(data);
        }

        // Detail
        [HttpGet("jobseeker/{id:int}")]
        public async Task<IActionResult> GetJobSeekerPostDetail([FromRoute] int id)
        {
            var data = await _svc.GetJobSeekerPostDetailAsync(id);
            return data is null ? NotFound() : Ok(data);
        }

        // Toggle Archive (Active <-> Archived)
        [HttpPost("jobseeker/{id:int}/toggle-archive")]
        public async Task<IActionResult> ToggleJobSeekerPostArchived([FromRoute] int id)
        {
            await _svc.ToggleJobSeekerPostArchivedAsync(id);
            return Ok(new { message = "JobSeeker post archive toggled." });
        }
    }
}
