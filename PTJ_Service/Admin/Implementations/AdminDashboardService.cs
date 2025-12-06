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

    public async Task<List<MonthlyUserStatsDto>> GetMonthlyUserStatsAsync()
    {
        return await _db.Users
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new MonthlyUserStatsDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();
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
    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync()
    {
        var result = await _db.EmployerTransactions
            .Where(t => t.Status == "Success")
            .GroupBy(t => new { t.PaidAt.Value.Year, t.PaidAt.Value.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Month = $"{g.Key.Month:00}/{g.Key.Year}",
                Revenue = g.Sum(t => (decimal?)t.Amount) ?? 0
            })
            .OrderBy(x => x.Month)
            .ToListAsync();

        return result;
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


}
