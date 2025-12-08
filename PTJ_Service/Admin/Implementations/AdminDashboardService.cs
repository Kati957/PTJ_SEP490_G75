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
            .Where(t => t.Status == "Paid")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var thisMonthRevenue = await _db.EmployerTransactions
            .Where(t => t.Status == "Paid" &&
                        t.PaidAt.Value.Month == thisMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var lastMonthRevenue = await _db.EmployerTransactions
            .Where(t => t.Status == "Paid" &&
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
            MonthGrowthPercent = Math.Round(growthPercent, 2)
        };
    }
    public async Task<List<RevenueStatsDto>> GetRevenueByDayAsync()
    {
        return await _db.EmployerTransactions
            .Where(t => t.Status == "Paid" && t.PaidAt != null)
            .GroupBy(t => t.PaidAt!.Value.Date)
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
            .Where(t => t.Status == "Paid" && t.PaidAt != null)
            .GroupBy(t => new
            {
                Year = t.PaidAt!.Value.Year,
                Month = t.PaidAt!.Value.Month
            })
            .Select(g => new RevenueStatsDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(e => (decimal?)e.Amount) ?? 0
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();
    }

    public async Task<List<RevenueStatsDto>> GetRevenueByYearAsync()
    {
        return await _db.EmployerTransactions
            .Where(t => t.Status == "Paid" && t.PaidAt != null)
            .GroupBy(t => t.PaidAt!.Value.Year)
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
        // Lấy toàn bộ danh sách kế hoạch
        var plans = await _db.EmployerPlans.ToListAsync();

        // Các giao dịch grouped theo PlanId
        var paidTransactions = await _db.EmployerTransactions
            .Where(t => t.Status == "Paid")
            .GroupBy(t => t.PlanId)
            .Select(g => new
            {
                PlanId = g.Key,
                Revenue = g.Sum(x => x.Amount ?? 0),
                Transactions = g.Count(),
                Users = g.Select(x => x.UserId).Distinct().Count()
            })
            .ToListAsync();

        // Tổng tất cả giao dịch (Paid + Cancelled + Pending)
        var allTransactions = await _db.EmployerTransactions
            .GroupBy(t => t.PlanId)
            .Select(g => new
            {
                PlanId = g.Key,
                TotalTransactions = g.Count()
            })
            .ToListAsync();

        var result = plans.Select(plan =>
        {
            var paid = paidTransactions.FirstOrDefault(x => x.PlanId == plan.PlanId);
            var total = allTransactions.FirstOrDefault(x => x.PlanId == plan.PlanId);

            int success = paid?.Transactions ?? 0;
            int totalTrans = total?.TotalTransactions ?? 0;

            decimal successRate = totalTrans == 0
                ? 0
                : Math.Round((decimal)success / totalTrans * 100, 2);

            return new RevenueByPlanDto
            {
                PlanName = plan.PlanName,
                Revenue = paid?.Revenue ?? 0,
                Transactions = paid?.Transactions ?? 0,
                Users = paid?.Users ?? 0,
                SuccessRate = successRate
            };
        })
        .ToList();

        return result;
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
