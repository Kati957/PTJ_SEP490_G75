using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTOs;
using PTJ_Models.Models;
using PTJ_Service.RatingService.Interfaces;

namespace PTJ_Service.RatingService.Implementations
{
    public class RatingService : IRatingService
    {
        private readonly JobMatchingDbContext _context;

        public RatingService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateRatingAsync(RatingCreateDto dto)
        {
            // Kiểm tra giá trị hợp lệ
            if (dto.RatingValue < 0 || dto.RatingValue > 5)
                throw new ArgumentException("Rating must be between 0 and 5");

            var rating = new Rating
            {
                RaterId = dto.RaterId,
                RateeId = dto.RateeId,
                SubmissionId = dto.SubmissionId,
                RatingValue = dto.RatingValue,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RatingViewDto>> GetRatingsForUserAsync(int rateeId)
        {
            return await _context.Ratings
                .Where(r => r.RateeId == rateeId)
                .Include(r => r.Rater)
                .Select(r => new RatingViewDto
                {
                    RatingId = r.RatingId,
                    RaterId = r.RaterId,
                    RaterName = r.Rater.Username,
                    RatingValue = r.RatingValue ?? 0,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<decimal> GetAverageRatingAsync(int rateeId)
        {
            var avg = await _context.Ratings
                .Where(r => r.RateeId == rateeId)
                .AverageAsync(r => r.RatingValue) ?? 0;

            return Math.Round(avg, 2);
        }
    }
}
