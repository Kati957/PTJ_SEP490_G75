using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.RatingDto;
using PTJ_Service.RatingService.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        // 🟢 [POST] /api/rating
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRating([FromBody] RatingCreateDto dto)
        {
            // ✅ Lấy userId từ token
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null)
                return Unauthorized(new { message = "Không thể xác định tài khoản đăng nhập." });

            int raterId = int.Parse(claim.Value);

            await _ratingService.CreateRatingAsync(dto, raterId);
            return Ok(new { message = "Đánh giá thành công!" });
        }

        // 🟡 [GET] /api/rating/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRatingsForUser(int userId)
        {
            var ratings = await _ratingService.GetRatingsForUserAsync(userId);
            return Ok(ratings);
        }

        // 🟣 [GET] /api/rating/user/{userId}/average
        [HttpGet("user/{userId}/average")]
        public async Task<IActionResult> GetAverageRating(int userId)
        {
            var avg = await _ratingService.GetAverageRatingAsync(userId);
            return Ok(new { userId, average = avg });
        }
    }
}
