using PTJ_Models.DTOs;

namespace PTJ_Service.RatingService.Interfaces
{
    public interface IRatingService
    {
        Task<bool> CreateRatingAsync(RatingCreateDto dto);
        Task<IEnumerable<RatingViewDto>> GetRatingsForUserAsync(int rateeId);
        Task<decimal> GetAverageRatingAsync(int rateeId);
    }
}
