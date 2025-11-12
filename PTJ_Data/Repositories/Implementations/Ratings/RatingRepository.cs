using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.Ratings;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Ratings
{
    public class RatingRepository : IRatingRepository
    {
        private readonly JobMatchingDbContext _context;

        public RatingRepository(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Rating rating)
        {
            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Rating>> GetRatingsByRateeAsync(int rateeId)
        {
            return await _context.Ratings
                .Where(r => r.RateeId == rateeId)
                .Include(r => r.Rater)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetAverageRatingByRateeAsync(int rateeId)
        {
            // ✅ Nếu không có rating thì trả về 0
            if (!await _context.Ratings.AnyAsync(r => r.RateeId == rateeId))
                return 0;

            // ✅ Fix decimal? → decimal bằng ?? 0
            var avg = await _context.Ratings
                .Where(r => r.RateeId == rateeId)
                .AverageAsync(r => r.RatingValue ?? 0);

            return Math.Round(avg, 2);
        }
    }
}
