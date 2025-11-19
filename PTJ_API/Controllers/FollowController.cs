using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "JobSeeker")] // chỉ JobSeeker được follow
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        // Lấy UserId từ token
        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue("UserId")
                ?? throw new Exception("UserId not found in token"));
        }

        [HttpPost("{employerId}")]
        public async Task<IActionResult> Follow(int employerId)
        {
            int jobSeekerId = GetUserId();
            await _followService.FollowEmployerAsync(jobSeekerId, employerId);

            return Ok(new { message = "Đã theo dõi nhà tuyển dụng thành công." });
        }

        [HttpDelete("{employerId}")]
        public async Task<IActionResult> Unfollow(int employerId)
        {
            int jobSeekerId = GetUserId();
            await _followService.UnfollowEmployerAsync(jobSeekerId, employerId);

            return Ok(new { message = "Bạn đã hủy theo dõi nhà tuyển dụng này." });
        }

        [HttpGet("check/{employerId}")]
        public async Task<IActionResult> CheckFollow(int employerId)
        {
            int jobSeekerId = GetUserId();
            var isFollowing = await _followService.IsFollowingAsync(jobSeekerId, employerId);

            return Ok(new { isFollowing });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFollowingList()
        {
            int jobSeekerId = GetUserId();
            var result = await _followService.GetFollowingListAsync(jobSeekerId);

            return Ok(result);
        }
    }
}
