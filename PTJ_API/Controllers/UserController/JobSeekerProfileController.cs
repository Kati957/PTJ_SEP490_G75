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
    public class JobSeekerProfileController : ControllerBase
        {
        private readonly IJobSeekerProfileService _jobSeekerService;
        private readonly IEmployerProfileService _employerService;

        public JobSeekerProfileController(
            IJobSeekerProfileService jobSeekerService,
            IEmployerProfileService employerService)
            {
            _jobSeekerService = jobSeekerService;
            _employerService = employerService;
            }

        // 👤 Lấy profile của chính JobSeeker đăng nhập
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _jobSeekerService.GetProfileAsync(userId);
            if (dto == null)
                return NotFound("Không tìm thấy profile.");
            return Ok(dto);
            }

        // 🌐 Xem public profile (JobSeeker hoặc Employer)
        [HttpGet("public/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileByUserId(int userId)
            {
            // ✅ Thử tìm JobSeeker trước
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

            // ✅ Nếu không có JobSeeker → thử Employer
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

            return NotFound("Không tìm thấy profile.");
            }

        // ✏️ Cập nhật thông tin (chỉ JobSeeker/Admin)
        [HttpPut("update")]
        [Authorize(Roles = "JobSeeker,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] JobSeekerProfileUpdateDto model)
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobSeekerService.UpdateProfileAsync(userId, model);
            if (!success)
                return BadRequest("Cập nhật thất bại.");
            return Ok("Cập nhật profile thành công.");
            }

        // ❌ Xóa ảnh đại diện (trả về mặc định)
        [HttpDelete("picture")]
        [Authorize(Roles = "JobSeeker,Admin")]
        public async Task<IActionResult> DeleteProfilePicture()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobSeekerService.DeleteProfilePictureAsync(userId);
            if (!success)
                return BadRequest("Không thể gỡ ảnh.");
            return Ok("Ảnh đã được thay bằng ảnh mặc định.");
            }
        }
    }
