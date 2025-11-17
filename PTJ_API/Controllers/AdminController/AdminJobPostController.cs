using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/jobposts")]
    [Authorize(Roles = "Admin")]
    public class AdminJobPostController : ControllerBase
    {
        private readonly IAdminJobPostService _svc;
        public AdminJobPostController(IAdminJobPostService svc) => _svc = svc;

        // Lấy AdminId từ token
        private int AdminId =>
            int.Parse(
                User.FindFirst("sub")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                throw new Exception("Token missing userId")
            );

        // ================================
        // EMPLOYER POSTS
        // ================================

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

        //  Admin toggle block + nhập lý do 
        [HttpPost("employer/{id:int}/toggle-block")]
        public async Task<IActionResult> ToggleEmployerPostBlocked(int id, [FromBody] BlockPostDto dto)
        {
            await _svc.ToggleEmployerPostBlockedAsync(id, dto.Reason, AdminId);
            return Ok(new { message = "Employer post status toggled." });
        }

        // ================================
        // JOB SEEKER POSTS
        // ================================

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

        //  Admin toggle archive + nhập lý do ⭐
        [HttpPost("jobseeker/{id:int}/toggle-archive")]
        public async Task<IActionResult> ToggleJobSeekerPostArchived(int id, [FromBody] BlockPostDto dto)
        {
            await _svc.ToggleJobSeekerPostArchivedAsync(id, dto.Reason, AdminId);
            return Ok(new { message = "JobSeeker post status toggled." });
        }
    }
}
