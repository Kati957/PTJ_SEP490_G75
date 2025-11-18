using Microsoft.AspNetCore.Mvc;
using PTJ_Service.FollowService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        [HttpPost("{employerId}")]
        public async Task<IActionResult> Follow(int employerId, [FromQuery] int jobSeekerId)
        {
            await _followService.FollowEmployerAsync(jobSeekerId, employerId);
            return Ok(new { message = "Đã theo dõi nhà tuyển dụng thành công." });
        }

        [HttpDelete("{employerId}")]
        public async Task<IActionResult> Unfollow(int employerId, [FromQuery] int jobSeekerId)
        {
            await _followService.UnfollowEmployerAsync(jobSeekerId, employerId);
            return Ok(new { message = "Bạn đã hủy theo dõi nhà tuyển dụng này." });
        }

        [HttpGet("check/{employerId}")]
        public async Task<IActionResult> CheckFollow(int employerId, [FromQuery] int jobSeekerId)
        {
            var isFollowing = await _followService.IsFollowingAsync(jobSeekerId, employerId);
            return Ok(new { isFollowing });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFollowingList([FromQuery] int jobSeekerId)
        {
            var result = await _followService.GetFollowingListAsync(jobSeekerId);
            return Ok(result);
        }
    }
}
