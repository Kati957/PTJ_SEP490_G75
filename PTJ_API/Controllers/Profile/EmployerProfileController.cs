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
        private readonly IJobSeekerProfileService _jobSeekerService;

        public EmployerProfileController(
            IEmployerProfileService employerService,
            IJobSeekerProfileService jobSeekerService)
            {
            _employerService = employerService;
            _jobSeekerService = jobSeekerService;
            }

        // 🧑‍💼 Lấy profile của chính Employer đang đăng nhập
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _employerService.GetProfileAsync(userId);
            if (dto == null)
                return NotFound("Không tìm thấy profile.");
            return Ok(dto);
            }

        // 🌐 Xem public profile (Employer hoặc JobSeeker)
        [HttpGet("public/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileByUserId(int userId)
            {
            // ✅ Thử tìm Employer trước
            var employerDto = await _employerService.GetProfileByUserIdAsync(userId);
            if (employerDto != null)
                {
                return Ok(new
                    {
                    Role = "Employer",
                    employerDto.DisplayName,
                    employerDto.Description,
                    employerDto.AvatarUrl,
                    employerDto.Website,
                    employerDto.ContactPhone,
                    employerDto.ContactEmail,
                    employerDto.Location
                    });
                }

            // ✅ Nếu không có Employer → thử JobSeeker
            var jobSeekerDto = await _jobSeekerService.GetProfileByUserIdAsync(userId);
            if (jobSeekerDto != null)
                {
                return Ok(new
                    {
                    Role = "JobSeeker",
                    DisplayName = jobSeekerDto.FullName,
                    AvatarUrl = jobSeekerDto.ProfilePicture,
                    jobSeekerDto.Gender,
                    jobSeekerDto.BirthYear,
                    jobSeekerDto.Skills,
                    jobSeekerDto.Experience,
                    jobSeekerDto.Education,
                    jobSeekerDto.PreferredJobType,
                    Location = jobSeekerDto.PreferredLocation,
                    jobSeekerDto.ContactPhone
                    });
                }

            return NotFound("Không tìm thấy profile.");
            }

        // ✏️ Cập nhật thông tin (chỉ Employer/Admin)
        [HttpPut("update")]
        [Authorize(Roles = "Employer,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] EmployerProfileUpdateDto model)
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _employerService.UpdateProfileAsync(userId, model);
            if (!success)
                return BadRequest("Cập nhật thất bại.");
            return Ok("Cập nhật profile thành công.");
            }

        // ❌ Xóa avatar (trả về ảnh mặc định)
        [HttpDelete("avatar")]
        [Authorize(Roles = "Employer,Admin")]
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
