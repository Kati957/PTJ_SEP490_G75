using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Service.SearchService.Interfaces;
using System.Security.Claims;

namespace PTJ_Service.SearchService.Implementations
    {
    public class SearchSuggestionService : ISearchSuggestionService
        {
        private readonly JobMatchingDbContext _db;
        private readonly IHttpContextAccessor _contextAccessor;

        public SearchSuggestionService(JobMatchingDbContext db, IHttpContextAccessor accessor)
            {
            _db = db;
            _contextAccessor = accessor;
            }

        // Lấy role từ token
        private string GetUserRole()
            {
            var user = _contextAccessor.HttpContext?.User;

            if (user == null || !user.Identity!.IsAuthenticated)
                return "JobSeeker"; // default for anonymous

            var role = user.FindFirst("role")?.Value
                    ?? user.FindFirst(ClaimTypes.Role)?.Value;

            return role ?? "JobSeeker"; // fallback
            }


        public async Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword)
            {
            var role = GetUserRole(); // Lấy role từ ID Token

            if (string.IsNullOrWhiteSpace(keyword))
                return await GetPopularKeywordsAsync(role);

            keyword = keyword.ToLower();
            List<string> results = new();

            // Employer → tìm ứng viên
            if (role == "Employer")
                {
                var fromTitle = await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active" && EF.Functions.Like(p.Title, $"%{keyword}%")
)
                    .Select(p => p.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var fromCategory = await _db.Categories
                    .Where(c => c.Name.ToLower().Contains(keyword))
                    .Select(c => c.Name)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                results.AddRange(fromTitle);
                results.AddRange(fromCategory);
                }
            // JobSeeker → tìm bài tuyển dụng
            else if (role == "JobSeeker")
                {
                var fromTitle = await _db.EmployerPosts
                    .Where(p => p.Status == "Active" && EF.Functions.Like(p.Title, $"%{keyword}%")
)
                    .Select(p => p.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var fromCategory = await _db.Categories
                    .Where(c => c.Name.ToLower().Contains(keyword))
                    .Select(c => c.Name)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                results.AddRange(fromTitle);
                results.AddRange(fromCategory);
                }
            // Không có role → tìm tất cả
            else
                {
                var fromEmployer = await _db.EmployerPosts
                    .Where(p => p.Status == "Active" && EF.Functions.Like(p.Title, $"%{keyword}%"))
                    .Select(p => p.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var fromSeeker = await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active" && EF.Functions.Like(p.Title, $"%{keyword}%"))
                    .Select(p => p.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var fromCategory = await _db.Categories
                    .Where(c => c.Name.ToLower().Contains(keyword))
                    .Select(c => c.Name)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                results.AddRange(fromEmployer);
                results.AddRange(fromSeeker);
                results.AddRange(fromCategory);
                }

            return results.Distinct().Take(10);
            }


        // Từ khóa phổ biến theo role token
        public async Task<IEnumerable<string>> GetPopularKeywordsAsync(string? role)
            {
            if (role == "Employer")
                {
                return await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(10)
                    .ToListAsync();
                }

            if (role == "JobSeeker")
                {
                return await _db.EmployerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(10)
                    .ToListAsync();
                }

            var employer = await _db.EmployerPosts
                .Where(p => p.Status == "Active")
                .Select(p => p.Title)
                .Take(10)
                .ToListAsync();

            var seeker = await _db.JobSeekerPosts
                .Where(p => p.Status == "Active")
                .Select(p => p.Title)
                .Take(10)
                .ToListAsync();

            return employer.Concat(seeker).Distinct().Take(10);
            }

        }
    }
