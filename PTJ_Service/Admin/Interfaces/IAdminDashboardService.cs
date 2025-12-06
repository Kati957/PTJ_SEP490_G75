using PTJ_Models.DTO.AdminDashbroad;

namespace PTJ_Service.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryDto> GetSummaryAsync();
        Task<List<MonthlyUserStatsDto>> GetMonthlyUserStatsAsync();
        Task<List<JobCategoryStatsDto>> GetJobCategoryStatsAsync();
        Task<List<TopEmployerStatsDto>> GetTopEmployersAsync();
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync();
        Task<RevenueSummaryDto> GetRevenueSummaryAsync();
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<RevenueByPlanDto>> GetRevenueByPlanAsync();
    }
}
