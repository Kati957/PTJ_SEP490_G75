using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;
using PTJ_Models.Models;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_Service.SearchService.Implementations
    {
    public class SearchSuggestionService : ISearchSuggestionService
        {
        private readonly JobMatchingDbContext _db;

        public SearchSuggestionService(JobMatchingDbContext db)
            {
            _db = db;
            }

        // Gợi ý theo keyword + role
        public async Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword, int? roleId)
            {
            // Nếu chưa nhập keyword → lấy phổ biến theo role
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetPopularKeywordsAsync(roleId);

            keyword = keyword.ToLower();
            List<string> results = new();

            // Employer → tìm ứng viên
            if (roleId == 2)
                {
                var fromTitle = await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active" && p.Title.ToLower().Contains(keyword))
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
            else if (roleId == 1)
                {
                var fromTitle = await _db.EmployerPosts
                    .Where(p => p.Status == "Active" && p.Title.ToLower().Contains(keyword))
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
            // Nếu role không xác định → gộp cả hai
            else
                {
                var fromEmployer = await _db.EmployerPosts
                    .Where(p => p.Status == "Active" && p.Title.ToLower().Contains(keyword))
                    .Select(p => p.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var fromSeeker = await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active" && p.Title.ToLower().Contains(keyword))
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

        // Từ khóa phổ biến theo role
        public async Task<IEnumerable<string>> GetPopularKeywordsAsync(int? roleId)
            {
            if (roleId == 2)
                {
                // Employer → lấy từ JobSeekerPosts
                return await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .Select(g => new { Keyword = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .Select(g => g.Keyword)
                    .ToListAsync();
                }

            if (roleId == 3)
                {
                // JobSeeker → lấy từ EmployerPosts
                return await _db.EmployerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .Select(g => new { Keyword = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .Select(g => g.Keyword)
                    .ToListAsync();
                }

            // Default → gộp cả hai
            var fromEmployer = await _db.EmployerPosts
                .Where(p => p.Status == "Active")
                .Select(p => p.Title)
                .Take(10)
                .ToListAsync();

            var fromSeeker = await _db.JobSeekerPosts
                .Where(p => p.Status == "Active")
                .Select(p => p.Title)
                .Take(10)
                .ToListAsync();

            return fromEmployer.Concat(fromSeeker).Distinct().Take(10);
            }
        }
    }
