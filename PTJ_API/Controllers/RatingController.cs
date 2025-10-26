using Microsoft.AspNetCore.Mvc;
using PTJ_Service.RatingService;
using PTJ_Models.DTOs;

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

        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] RatingCreateDto dto)
        {
            await _ratingService.CreateRatingAsync(dto);
            return Ok(new { message = "Rating created successfully" });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRatingsForUser(int userId)
        {
            var ratings = await _ratingService.GetRatingsForUserAsync(userId);
            return Ok(ratings);
        }

        [HttpGet("user/{userId}/average")]
        public async Task<IActionResult> GetAverageRating(int userId)
        {
            var avg = await _ratingService.GetAverageRatingAsync(userId);
            return Ok(new { userId, average = avg });
        }
    }
}
