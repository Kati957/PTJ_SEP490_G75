using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/employers")]
    public class EmployerRankingController : ControllerBase
    {
        private readonly IEmployerRankingService _svc;

        public EmployerRankingController(IEmployerRankingService svc)
        {
            _svc = svc;
        }

        // GET: /api/employers/top?top=10
        [HttpGet("top")]
        public async Task<IActionResult> GetTopEmployers([FromQuery] int top = 10)
        {
            var data = await _svc.GetTopEmployersAsync(top);
            return Ok(data);
        }
    }
}
