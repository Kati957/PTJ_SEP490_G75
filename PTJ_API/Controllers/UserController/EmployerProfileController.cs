using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployerProfileController : ControllerBase
        {
        private readonly IEmployerProfileService _service;

        public EmployerProfileController(IEmployerProfileService service)
            {
            _service = service;
            }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _service.GetProfileAsync(userId);
            if (dto == null) return NotFound("Không tìm thấy profile.");
            return Ok(dto);
            }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProfiles()
            {
            var list = await _service.GetAllProfilesAsync();
            return Ok(list);
            }

        [HttpGet("{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileByUserId(int userId)
            {
            var dto = await _service.GetProfileByUserIdAsync(userId);
            if (dto == null) return NotFound("Không tìm thấy profile.");
            return Ok(dto);
            }

        [HttpPut("update")]
        [Authorize(Roles = "Employer,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] EmployerProfileUpdateDto model)
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _service.UpdateProfileAsync(userId, model);
            if (!success) return BadRequest("Cập nhật thất bại.");
            return Ok("Cập nhật profile thành công.");
            }

        [HttpDelete("avatar")]
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> DeleteAvatar()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _service.DeleteAvatarAsync(userId);
            if (!success) return BadRequest("Không thể gỡ ảnh.");
            return Ok("Ảnh đại diện đã được thay bằng ảnh mặc định.");
            }
        }
    }
