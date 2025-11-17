using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Ratings
{
    public interface IRatingRepository
    {
        Task AddAsync(Rating rating);
        Task<IEnumerable<Rating>> GetRatingsByRateeAsync(int rateeId);
        Task<decimal> GetAverageRatingByRateeAsync(int rateeId);
    }
}
