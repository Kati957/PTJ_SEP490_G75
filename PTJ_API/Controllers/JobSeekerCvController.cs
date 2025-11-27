using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.CvDTO;
using PTJ_Service.JobSeekerCvService.Interfaces;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "JobSeeker,Admin")]
    public class JobSeekerCvController : ControllerBase
        {
        private readonly IJobSeekerCvService _service;

        public JobSeekerCvController(IJobSeekerCvService service)
            {
            _service = service;
            }


        // Lấy danh sách CV của ứng viên

        [HttpGet]
        public async Task<IActionResult> GetMyCvs()
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int jobSeekerId = int.Parse(sub.Value);

            var result = await _service.GetByJobSeekerAsync(jobSeekerId);
            return Ok(new { success = true, total = result.Count(), data = result });
            }


        // Xem chi tiết CV

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int jobSeekerId = int.Parse(sub.Value);

            var result = await _service.GetByIdAsync(id, jobSeekerId);

            if (result == null)
                return NotFound(new { success = false, message = "Không tìm thấy CV." });

            return Ok(new { success = true, data = result });
            }



        // Tạo CV mới

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobSeekerCvCreateDto dto)
            {
            if (!ModelState.IsValid)
                {
                return BadRequest(new
                    {
                    success = false,
                    message = "Dữ liệu không hợp lệ.",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int jobSeekerId = int.Parse(sub.Value);
            var result = await _service.CreateAsync(jobSeekerId, dto);

            return Ok(new { success = true, message = "Tạo CV thành công.", data = result });
            }


        // Cập nhật CV

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] JobSeekerCvUpdateDto dto)
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int jobSeekerId = int.Parse(sub.Value);
            var result = await _service.UpdateAsync(jobSeekerId, id, dto);

            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy CV hoặc bạn không có quyền." });

            return Ok(new { success = true, message = "Cập nhật CV thành công." });
            }


        // Xóa CV

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (sub == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int jobSeekerId = int.Parse(sub.Value);
            var result = await _service.DeleteAsync(jobSeekerId, id);

            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy CV hoặc bạn không có quyền." });

            return Ok(new { success = true, message = "Xóa CV thành công." });
            }
        }
    }
