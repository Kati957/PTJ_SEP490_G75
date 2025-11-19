using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_API.Controllers.AdminController
{
    [Route("api/admin/statistics")]
    [ApiController]
    public class AdminStatisticsController : ControllerBase
    {
        private readonly IAdminStatisticsService _service;

        public AdminStatisticsController(IAdminStatisticsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            var stats = await _service.GetAdminStatisticsAsync();
            return Ok(stats);
        }
    }
}
