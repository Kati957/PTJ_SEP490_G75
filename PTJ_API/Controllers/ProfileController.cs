using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.ProfileService;

namespace PTJ_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var profile = await _profileService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound("User not found");
            return Ok(profile);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(int userId, [FromBody] ProfileDto dto)
        {
            if (dto == null) return BadRequest("Invalid profile data");
            var success = await _profileService.UpdateProfileAsync(userId, dto);
            if (!success) return BadRequest("Update failed");
            return Ok("Profile updated successfully");
        }
    }
}
