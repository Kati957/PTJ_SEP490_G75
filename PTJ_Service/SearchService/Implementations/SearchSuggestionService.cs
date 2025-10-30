using Microsoft.EntityFrameworkCore;
using PTJ_Data;
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

        // Gợi ý theo roleId
        public async Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword, int? roleId)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetPopularKeywordsAsync(roleId);

            keyword = keyword.ToLower();

            List<string> results = new();

            //  Employer (roleId = 2) tìm ứng viên
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
            //  JobSeeker (roleId = 1) tìm bài tuyển dụng
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
            else
            {
                // Nếu roleId khác hoặc null => lấy cả hai loại
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

        // Từ khóa phổ biến theo roleId
        public async Task<IEnumerable<string>> GetPopularKeywordsAsync(int? roleId)
        {
            if (roleId == 2)
            {
                // Employer → lấy phổ biến từ JobSeekerPosts
                return await _db.JobSeekerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .Select(g => new { Keyword = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .Select(g => g.Keyword)
                    .ToListAsync();
            }

            if (roleId == 1)
            {
                // JobSeeker → lấy phổ biến từ EmployerPosts
                return await _db.EmployerPosts
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.Title)
                    .Select(g => new { Keyword = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .Select(g => g.Keyword)
                    .ToListAsync();
            }

            // Default → gộp cả 2
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
