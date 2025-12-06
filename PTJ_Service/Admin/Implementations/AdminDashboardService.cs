using Microsoft.EntityFrameworkCore;
using PTJ_Service.Interfaces;
using PTJ_Models.Models;
using PTJ_Models.DTO.AdminDashbroad;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly JobMatchingDbContext _db;

    public AdminDashboardService(JobMatchingDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardSummaryDto> GetSummaryAsync()
    {
        return new AdminDashboardSummaryDto
        {
            TotalUsers = await _db.Users.CountAsync(),

            NewUsers30Days = await _db.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)),

            TotalEmployers = await _db.Users
                .CountAsync(u => u.Roles.Any(r => r.RoleName == "Employer")),

            TotalJobSeekers = await _db.Users
                .CountAsync(u => u.Roles.Any(r => r.RoleName == "JobSeeker")),

            TotalPosts = await _db.EmployerPosts.CountAsync(),
            ActivePosts = await _db.EmployerPosts.CountAsync(p => p.Status == "Active"),
            PendingPosts = await _db.EmployerPosts.CountAsync(p => p.Status == "Pending"),

            PendingReports = await _db.PostReports.CountAsync(r => r.Status == "Pending"),
            SolvedReports = await _db.PostReportSolveds.CountAsync(),

            TotalApplications = await _db.JobSeekerSubmissions.CountAsync(),
            NewApplications30Days = await _db.JobSeekerSubmissions
                .CountAsync(a => a.AppliedAt >= DateTime.UtcNow.AddDays(-30))
        };
    }
    public async Task<List<UserStatsByDayDto>> GetUserStatsByDayAsync()
    {
        var employers = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "Employer"))
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var seekers = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "JobSeeker"))
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var allDates = employers.Select(e => e.Date)
            .Union(seekers.Select(s => s.Date))
            .Distinct();

        return allDates
            .Select(d => new UserStatsByDayDto
            {
                Date = d,
                Employers = employers.FirstOrDefault(e => e.Date == d)?.Count ?? 0,
                JobSeekers = seekers.FirstOrDefault(s => s.Date == d)?.Count ?? 0
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<List<UserStatsByMonthDto>> GetUserStatsByMonthAsync()
    {
        var emp = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "Employer"))
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var js = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "JobSeeker"))
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var all = emp
            .Select(e => new { e.Year, e.Month })
            .Union(js.Select(s => new { s.Year, s.Month }))
            .Distinct();

        return all
            .Select(x => new UserStatsByMonthDto
            {
                Year = x.Year,
                Month = x.Month,
                Employers = emp.FirstOrDefault(e => e.Year == x.Year && e.Month == x.Month)?.Count ?? 0,
                JobSeekers = js.FirstOrDefault(s => s.Year == x.Year && s.Month == x.Month)?.Count ?? 0
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();
    }

    public async Task<List<UserStatsByYearDto>> GetUserStatsByYearAsync()
    {
        var emp = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "Employer"))
            .GroupBy(u => u.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync();

        var js = await _db.Users
            .Where(u => u.Roles.Any(r => r.RoleName == "JobSeeker"))
            .GroupBy(u => u.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync();

        var years = emp.Select(e => e.Year)
            .Union(js.Select(s => s.Year))
            .Distinct();

        return years
            .Select(y => new UserStatsByYearDto
            {
                Year = y,
                Employers = emp.FirstOrDefault(e => e.Year == y)?.Count ?? 0,
                JobSeekers = js.FirstOrDefault(s => s.Year == y)?.Count ?? 0
            })
            .OrderBy(x => x.Year).ToList();
    }

    public async Task<List<JobCategoryStatsDto>> GetJobCategoryStatsAsync()
    {
        return await _db.EmployerPosts
     .GroupBy(p => p.CategoryId)
     .Select(g => new JobCategoryStatsDto
     {
         CategoryId = g.Key ?? 0,
         CategoryName = g.First().Category != null
             ? g.First().Category.Name
             : "Không có danh mục",
         Count = g.Count()
     })
     .OrderByDescending(x => x.Count)
     .ToListAsync();

    }

    public async Task<List<TopEmployerStatsDto>> GetTopEmployersAsync()
    {
        return await _db.EmployerPosts
            .GroupBy(p => p.UserId)
            .Select(g => new TopEmployerStatsDto
            {
                UserId = g.Key,
                EmployerName = g.First().User.EmployerProfile.DisplayName,
                TotalPosts = g.Count(),
                TotalApplications = _db.JobSeekerSubmissions
                    .Count(a => a.EmployerPostId == g.First().EmployerPostId)
            })
            .OrderByDescending(x => x.TotalPosts)
            .Take(5)
            .ToListAsync();
    }
    public async Task<SubscriptionStatsDto> GetSubscriptionStatsAsync()
    {
        var activeSubs = await (
            from s in _db.EmployerSubscriptions
            join p in _db.EmployerPlans on s.PlanId equals p.PlanId
            where s.Status == "Active"
            group s by p.PlanName into g
            select new
            {
                PlanName = g.Key,
                Count = g.Count()
            }
        ).ToListAsync();

        int totalEmployers = await _db.Users.CountAsync(u =>
            u.Roles.Any(r => r.RoleName == "Employer"));

        int activeSubscribers = activeSubs.Sum(x => x.Count);
        int free = totalEmployers - activeSubscribers;

        return new SubscriptionStatsDto
        {
            Free = free,
            Medium = activeSubs.FirstOrDefault(x => x.PlanName == "Medium")?.Count ?? 0,
            Premium = activeSubs.FirstOrDefault(x => x.PlanName == "Premium")?.Count ?? 0,
            Active = activeSubscribers,
            Expired = await _db.EmployerSubscriptions.CountAsync(s => s.Status == "Expired")
        };
    }
    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonth = now.Month;
        var lastMonth = now.AddMonths(-1).Month;

        var totalRevenue = await _db.EmployerTransactions
            .Where(t => t.Status == "Success")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var thisMonthRevenue = await _db.EmployerTransactions
            .Where(t => t.Status == "Success" &&
                        t.PaidAt.Value.Month == thisMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var lastMonthRevenue = await _db.EmployerTransactions
            .Where(t => t.Status == "Success" &&
                        t.PaidAt.Value.Month == lastMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        decimal growthPercent = lastMonthRevenue == 0
            ? 100
            : ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;

        return new RevenueSummaryDto
        {
            TotalRevenue = totalRevenue,
            ThisMonthRevenue = thisMonthRevenue,
            LastMonthRevenue = lastMonthRevenue,
            GrowthPercent = Math.Round(growthPercent, 2)
        };
    }
    public async Task<List<RevenueStatsDto>> GetRevenueByDayAsync()
    {
        return await _db.EmployerTransactions
            .Where(t => t.Status == "Success" && t.PaidAt != null)
            .GroupBy(t => t.PaidAt.Value.Date)
            .Select(g => new RevenueStatsDto
            {
                Date = g.Key,
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(e => (decimal?)e.Amount) ?? 0
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<RevenueStatsDto>> GetRevenueByMonthAsync()
    {
        return await _db.EmployerTransactions
            .Where(t => t.Status == "Success" && t.PaidAt != null)
            .GroupBy(t => new { t.PaidAt.Value.Year, t.PaidAt.Value.Month })
            .Select(g => new RevenueStatsDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(e => (decimal?)e.Amount) ?? 0
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();
    }
    public async Task<List<RevenueStatsDto>> GetRevenueByYearAsync()
    {
        return await _db.EmployerTransactions
            .Where(t => t.Status == "Success" && t.PaidAt != null)
            .GroupBy(t => t.PaidAt.Value.Year)
            .Select(g => new RevenueStatsDto
            {
                Year = g.Key,
                Revenue = g.Sum(e => (decimal?)e.Amount) ?? 0
            })
            .OrderBy(x => x.Year)
            .ToListAsync();
    }


    public async Task<List<RevenueByPlanDto>> GetRevenueByPlanAsync()
    {
        var query = await (
            from t in _db.EmployerTransactions
            join p in _db.EmployerPlans on t.PlanId equals p.PlanId
            where t.Status == "Success"
            group t by p.PlanName into g
            select new RevenueByPlanDto
            {
                PlanName = g.Key,
                Revenue = g.Sum(t => (decimal?)t.Amount) ?? 0,
                Count = g.Count()
            }
        ).ToListAsync();

        return query;
    }
    public async Task<List<PostStatsByDayDto>> GetPostStatsByDayAsync()
    {
        var employer = await _db.EmployerPosts
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var seeker = await _db.JobSeekerPosts
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = employer
            .Union(seeker)
            .GroupBy(x => x.Date)
            .Select(g => new PostStatsByDayDto
            {
                Date = g.Key,
                EmployerPosts = employer.FirstOrDefault(e => e.Date == g.Key)?.Count ?? 0,
                JobSeekerPosts = seeker.FirstOrDefault(s => s.Date == g.Key)?.Count ?? 0,
            })
            .OrderBy(r => r.Date)
            .ToList();

        return result;
    }
    public async Task<List<PostStatsByMonthDto>> GetPostStatsByMonthAsync()
    {
        var employer = await _db.EmployerPosts
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var seeker = await _db.JobSeekerPosts
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var result = employer
            .Union(seeker)
            .GroupBy(x => new { x.Year, x.Month })
            .Select(g => new PostStatsByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                EmployerPosts = employer.FirstOrDefault(e => e.Year == g.Key.Year && e.Month == g.Key.Month)?.Count ?? 0,
                JobSeekerPosts = seeker.FirstOrDefault(s => s.Year == g.Key.Year && s.Month == g.Key.Month)?.Count ?? 0
            })
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ToList();

        return result;
    }
    public async Task<List<PostStatsByYearDto>> GetPostStatsByYearAsync()
    {
        var employer = await _db.EmployerPosts
            .GroupBy(p => p.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync();

        var seeker = await _db.JobSeekerPosts
            .GroupBy(p => p.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync();

        var years = employer.Select(e => e.Year)
                     .Union(seeker.Select(s => s.Year))
                     .Distinct();

        var result = years
            .Select(y => new PostStatsByYearDto
            {
                Year = y,
                EmployerPosts = employer.FirstOrDefault(e => e.Year == y)?.Count ?? 0,
                JobSeekerPosts = seeker.FirstOrDefault(s => s.Year == y)?.Count ?? 0
            })
            .OrderBy(x => x.Year)
            .ToList();

        return result;
    }
    public async Task<List<NewsStatsDto>> GetNewsStatsByDayAsync()
    {
        return await _db.News
            .GroupBy(n => n.CreatedAt.Date)
            .Select(g => new NewsStatsDto
            {
                Date = g.Key,
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }
    public async Task<List<NewsStatsDto>> GetNewsStatsByMonthAsync()
    {
        return await _db.News
            .GroupBy(n => new { n.CreatedAt.Year, n.CreatedAt.Month })
            .Select(g => new NewsStatsDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();
    }
    public async Task<List<NewsStatsDto>> GetNewsStatsByYearAsync()
    {
        return await _db.News
            .GroupBy(n => n.CreatedAt.Year)
            .Select(g => new NewsStatsDto
            {
                Year = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ToListAsync();
    }


}
