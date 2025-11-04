using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.SearchDTO;
using PTJ_Models.Models;
using PTJ_Service.SearchService.Interfaces;
using PTJ_Service.SearchService.Services;
using System.Security.Claims;

namespace PTJ_API.Controllers
    {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu đăng nhập cho tất cả
    public class SearchController : ControllerBase
        {
        private readonly IEmployerSearchService _employerSearch;
        private readonly IJobSeekerSearchService _jobSeekerSearch;
        private readonly ISearchSuggestionService _suggestionService;
        private readonly ICategoryService _categoryService;
        public SearchController(
            IEmployerSearchService employerSearch,
            IJobSeekerSearchService jobSeekerSearch,
            ISearchSuggestionService suggestionService,
            ICategoryService categoryService)
            {
            _employerSearch = employerSearch;
            _jobSeekerSearch = jobSeekerSearch;
            _suggestionService = suggestionService;
            _categoryService = categoryService;
            }

        // 🔹 Employer tìm JobSeeker
        [HttpPost("jobseekers")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> SearchJobSeekers([FromBody] EmployerSearchFilterDto filter)
            {
            var result = await _employerSearch.SearchJobSeekersAsync(filter);
            return Ok(result);
            }

        // 🔹 JobSeeker tìm EmployerPost
        [HttpPost("employerposts")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> SearchEmployerPosts([FromBody] JobSeekerSearchFilterDto filter)
            {
            var result = await _jobSeekerSearch.SearchEmployerPostsAsync(filter);
            return Ok(result);
            }

        // 🔹 Gợi ý từ khóa — lấy role tự động
        [HttpGet("suggestions")]
        [Authorize(Roles = "JobSeeker,Employer")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? keyword)
            {
            int? roleId = GetRoleIdFromClaims();
            var suggestions = await _suggestionService.GetSuggestionsAsync(keyword, roleId);
            return Ok(suggestions);
            }

        // 🔹 Từ khóa phổ biến — lấy role tự động
        [HttpGet("popular")]
        [Authorize(Roles = "JobSeeker,Employer")]
        public async Task<IActionResult> GetPopularKeywords()
            {
            int? roleId = GetRoleIdFromClaims();
            var popular = await _suggestionService.GetPopularKeywordsAsync(roleId);
            return Ok(popular);
            }

        // 🧠 Helper: Lấy roleId từ JWT
        private int? GetRoleIdFromClaims()
            {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return role switch
                {
                    "Employer" => 2,  // Nhà tuyển dụng
                    "JobSeeker" => 3, // Ứng viên
                    _ => null
                    };
            }
        }
    }
