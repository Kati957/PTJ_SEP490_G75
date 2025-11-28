using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Employer;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class EmployerRankingRepository : IEmployerRankingRepository
    {
        private readonly JobMatchingDbContext _db;

        public EmployerRankingRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<List<EmployerRankingDto>> GetTopEmployersByApplyCountAsync(int top)
        {
            var activePosts = await _db.EmployerPosts
                .Where(p => p.Status == "Active")
                .ToListAsync();

            // Group posts by employer (UserId)
            var grouped = activePosts
                .GroupBy(p => p.UserId)
                .ToList();

            var result = new List<EmployerRankingDto>();

            foreach (var g in grouped)
            {
                int employerId = g.Key;

                // Lấy profile employer
                var profile = await _db.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.UserId == employerId);

                // Tính tổng apply
                int applyCount = await _db.JobSeekerSubmissions
                    .CountAsync(s => g.Select(p => p.EmployerPostId)
                                      .Contains(s.EmployerPostId));

                result.Add(new EmployerRankingDto
                {
                    EmployerId = employerId,
                    CompanyName = profile?.DisplayName ?? "Unknown",
                    LogoUrl = profile?.AvatarUrl,
                    TotalApplyCount = applyCount,
                    ActivePostCount = g.Count()
                });
            }

            return result
                .OrderByDescending(x => x.TotalApplyCount)
                .ThenByDescending(x => x.ActivePostCount)
                .Take(top)
                .ToList();
        }
    }
}
