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

        public JobSeekerProfileController(IJobSeekerProfileService jobSeekerService)
            {
            _jobSeekerService = jobSeekerService;
            }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _jobSeekerService.GetProfileAsync(userId);
            return dto == null ? NotFound() : Ok(dto);
            }

        [HttpGet("public/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicProfile(int userId)
            {
            var dto = await _jobSeekerService.GetProfileByUserIdAsync(userId);
            return dto == null ? NotFound() : Ok(dto);
            }

        [HttpPut("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] JobSeekerProfileUpdateDto dto)
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobSeekerService.UpdateProfileAsync(userId, dto);
            return success ? Ok("Cập nhật thành công.") : BadRequest("Lỗi cập nhật.");
            }

        [HttpDelete("picture")]
        public async Task<IActionResult> RemovePicture()
            {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobSeekerService.DeleteProfilePictureAsync(userId);
            return success ? Ok("Đã reset ảnh đại diện.") : BadRequest("Lỗi reset ảnh.");
            }
        }
    }
