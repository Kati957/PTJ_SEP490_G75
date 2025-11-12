using PTJ_Models.DTO.RatingDto;

namespace PTJ_Service.RatingService.Interfaces
{
    public interface IRatingService
    {
        Task<bool> CreateRatingAsync(RatingCreateDto dto, int raterId);
        Task<IEnumerable<RatingViewDto>> GetRatingsForUserAsync(int rateeId);
        Task<decimal> GetAverageRatingAsync(int rateeId);
    }
}
