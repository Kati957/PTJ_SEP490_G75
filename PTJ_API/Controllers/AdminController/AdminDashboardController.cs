using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces;

namespace PTJ_API.Controllers.Admin
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PTJ_Service.Interfaces;

    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _svc;

        public AdminDashboardController(IAdminDashboardService svc)
        {
            _svc = svc;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(new { data = await _svc.GetSummaryAsync() });
        }

        [HttpGet("user-stats")]
        public async Task<IActionResult> GetUserStats()
        {
            return Ok(new { data = await _svc.GetMonthlyUserStatsAsync() });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategoryStats()
        {
            return Ok(new { data = await _svc.GetJobCategoryStatsAsync() });
        }

        [HttpGet("top-employers")]
        public async Task<IActionResult> GetTopEmployers()
        {
            return Ok(new { data = await _svc.GetTopEmployersAsync() });
        }

        [HttpGet("subscription-stats")]
        public async Task<IActionResult> GetSubscriptionStats()
        {
            var data = await _svc.GetSubscriptionStatsAsync();
            return Ok(new { data });
        }
        [HttpGet("revenue/summary")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            return Ok(await _svc.GetRevenueSummaryAsync());
        }

        [HttpGet("revenue/monthly")]
        public async Task<IActionResult> GetMonthlyRevenue()
        {
            return Ok(await _svc.GetMonthlyRevenueAsync());
        }

        [HttpGet("revenue/by-plan")]
        public async Task<IActionResult> GetRevenueByPlan()
        {
            return Ok(await _svc.GetRevenueByPlanAsync());
        }

    }

}
