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

        //  Employer Posts 

        // GET: list + filter + pagination
        [HttpGet("employer")]
        public async Task<IActionResult> GetEmployerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            PagedResult<AdminEmployerPostDto> data =
                await _svc.GetEmployerPostsAsync(status, categoryId, keyword, page, pageSize);
            return Ok(data);
        }

        // GET: detail
        [HttpGet("employer/{id:int}")]
        public async Task<IActionResult> GetEmployerPostDetail(int id)
        {
            var data = await _svc.GetEmployerPostDetailAsync(id);
            return data is null
                ? NotFound(new { message = $"Employer post {id} not found." })
                : Ok(data);
        }

        // POST: toggle block (Active <-> Blocked) => return new status
        [HttpPost("employer/{id:int}/toggle-block")]
        public async Task<IActionResult> ToggleEmployerPostBlocked(int id)
        {
            var newStatus = await _svc.ToggleEmployerPostBlockedAsync(id);
            return Ok(new { message = "Employer post status updated.", status = newStatus });
        }

        //  JobSeeker Posts 

        // GET: list + filter + pagination
        [HttpGet("jobseeker")]
        public async Task<IActionResult> GetJobSeekerPosts(
            [FromQuery] string? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            PagedResult<AdminJobSeekerPostDto> data =
                await _svc.GetJobSeekerPostsAsync(status, categoryId, keyword, page, pageSize);
            return Ok(data);
        }

        // GET: detail
        [HttpGet("jobseeker/{id:int}")]
        public async Task<IActionResult> GetJobSeekerPostDetail(int id)
        {
            var data = await _svc.GetJobSeekerPostDetailAsync(id);
            return data is null
                ? NotFound(new { message = $"JobSeeker post {id} not found." })
                : Ok(data);
        }

        // POST: toggle archive (Active <-> Archived) => return new status
        [HttpPost("jobseeker/{id:int}/toggle-archive")]
        public async Task<IActionResult> ToggleJobSeekerPostArchived(int id)
        {
            var newStatus = await _svc.ToggleJobSeekerPostArchivedAsync(id);
            return Ok(new { message = "JobSeeker post status updated.", status = newStatus });
        }
    }
}
