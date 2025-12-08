using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces;

namespace PTJ_API.Controllers.Admin
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PTJ_Service.AiService.Implementations;
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

        [HttpGet("users/by-day")]
        public async Task<IActionResult> GetUsersByDay()
        {
            return Ok(await _svc.GetUserStatsByDayAsync());
        }

        [HttpGet("users/by-month")]
        public async Task<IActionResult> GetUsersByMonth()
        {
            return Ok(await _svc.GetUserStatsByMonthAsync());
        }

        [HttpGet("users/by-year")]
        public async Task<IActionResult> GetUsersByYear()
        {
            return Ok(await _svc.GetUserStatsByYearAsync());
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

        [HttpGet("revenue/by-plan")]
        public async Task<IActionResult> GetRevenueByPlan()
        {
            var data = await _svc.GetRevenueByPlanAsync();
            return Ok(data);
        }

        [HttpGet("posts/by-day")]
        public async Task<IActionResult> GetPostStatsByDay()
        {
            var data = await _svc.GetPostStatsByDayAsync();
            return Ok(data);
        }

        [HttpGet("posts/by-month")]
        public async Task<IActionResult> GetPostStatsByMonth()
        {
            var data = await _svc.GetPostStatsByMonthAsync();
            return Ok(data);
        }

        [HttpGet("posts/by-year")]
        public async Task<IActionResult> GetPostStatsByYear()
        {
            var data = await _svc.GetPostStatsByYearAsync();
            return Ok(data);
        }
        [HttpGet("news/by-day")]
        public async Task<IActionResult> GetNewsByDay()
        {
            return Ok(await _svc.GetNewsStatsByDayAsync());
        }

        [HttpGet("news/by-month")]
        public async Task<IActionResult> GetNewsByMonth()
        {
            return Ok(await _svc.GetNewsStatsByMonthAsync());
        }

        [HttpGet("news/by-year")]
        public async Task<IActionResult> GetNewsByYear()
        {
            return Ok(await _svc.GetNewsStatsByYearAsync());
        }
        [HttpGet("revenue/by-day")]
        public async Task<IActionResult> GetRevenueByDay()
        {
            return Ok(await _svc.GetRevenueByDayAsync());
        }

        [HttpGet("revenue/by-month")]
        public async Task<IActionResult> GetRevenueByMonth()
        {
            return Ok(await _svc.GetRevenueByMonthAsync());
        }

        [HttpGet("revenue/by-year")]
        public async Task<IActionResult> GetRevenueByYear()
        {
            return Ok(await _svc.GetRevenueByYearAsync());
        }

    }

}
