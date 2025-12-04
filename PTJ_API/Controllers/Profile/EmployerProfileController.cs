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
        private readonly IEmployerProfileService _employerService;
       public EmployerProfileController(IEmployerProfileService employerService)
        {
            _employerService = employerService;
        }


        [HttpGet("me")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Không xác định được tài khoản.");

            var dto = await _employerService.GetProfileAsync(userId);

            if (dto == null)
                return NotFound("Không tìm thấy profile nhà tuyển dụng.");

            return Ok(dto);
        }


        // GET: api/employer-profile/{userId}
        [HttpGet("{userId:int}")]
        [AllowAnonymous]                    
        public async Task<IActionResult> GetPublicEmployerProfile(int userId)
        {
            var employer = await _employerService.GetProfileByUserIdAsync(userId);
            if (employer == null)
                return NotFound("Không tìm thấy hồ sơ nhà tuyển dụng.");

            return Ok(new
            {
                UserId = employer.UserId,
                Role = "Employer",

                employer.DisplayName,
                employer.Description,
                AvatarUrl = employer.AvatarUrl,
                employer.Website,
                employer.ContactPhone,
                employer.ContactEmail,
                Location = employer.Location
            });
        }

        // Cập nhật thông tin (chỉ Employer/Admin)
        [HttpPut("update")]
        [Authorize(Roles = "Employer")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] EmployerProfileUpdateDto model)
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _employerService.UpdateProfileAsync(userId, model);

            if (!success)
                return BadRequest("Cập nhật thất bại.");

            return Ok("Cập nhật profile thành công.");
            }

        //  Xóa avatar (trả về ảnh mặc định)
        [HttpDelete("avatar")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> DeleteAvatar()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _employerService.DeleteAvatarAsync(userId);

            if (!success)
                return BadRequest("Không thể gỡ ảnh.");

            return Ok("Ảnh đại diện đã được thay bằng ảnh mặc định.");
            }
        }
    }
