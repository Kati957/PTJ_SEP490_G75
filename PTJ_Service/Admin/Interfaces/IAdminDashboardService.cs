using PTJ_Models.DTO.AdminDashbroad;

namespace PTJ_Service.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryDto> GetSummaryAsync();
        Task<List<UserStatsByDayDto>> GetUserStatsByDayAsync();
        Task<List<UserStatsByMonthDto>> GetUserStatsByMonthAsync();
        Task<List<UserStatsByYearDto>> GetUserStatsByYearAsync();
        Task<List<JobCategoryStatsDto>> GetJobCategoryStatsAsync();
        Task<List<TopEmployerStatsDto>> GetTopEmployersAsync();
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync();
        Task<RevenueSummaryDto> GetRevenueSummaryAsync();
        Task<List<RevenueByPlanDto>> GetRevenueByPlanAsync();
        Task<List<PostStatsByDayDto>> GetPostStatsByDayAsync();
        Task<List<PostStatsByMonthDto>> GetPostStatsByMonthAsync();
        Task<List<PostStatsByYearDto>> GetPostStatsByYearAsync();
        Task<List<NewsStatsDto>> GetNewsStatsByDayAsync();
        Task<List<NewsStatsDto>> GetNewsStatsByMonthAsync();
        Task<List<NewsStatsDto>> GetNewsStatsByYearAsync();
        Task<List<RevenueStatsDto>> GetRevenueByDayAsync();
        Task<List<RevenueStatsDto>> GetRevenueByMonthAsync();
        Task<List<RevenueStatsDto>> GetRevenueByYearAsync();
    }
}
