using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.JobSeekerCvService.Interfaces;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/employer/cv")]
    [Authorize(Roles = "Employer,Admin")]
    public class EmployerCvController : ControllerBase
        {
        private readonly IJobSeekerCvService _service;

        public EmployerCvController(IJobSeekerCvService service)
            {
            _service = service;
            }

        // Employer xem CV
        [HttpGet("{cvId}")]
        public async Task<IActionResult> ViewCv(int cvId)
            {
            var data = await _service.GetByIdForEmployerAsync(cvId);

            if (data == null)
                return NotFound(new { success = false, message = "Không tìm thấy CV." });

            return Ok(new { success = true, data });
            }
        }
    }
