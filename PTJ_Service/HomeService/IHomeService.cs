using PTJ_Models.DTO;
using PTJ_Models.DTO.HomePageDTO;

namespace PTJ_Service.HomeService
{
    public interface IHomeService
    {
        Task<IEnumerable<HomePostDto>> GetLatestPostsAsync(string? keyword, int page, int pageSize);
        Task<HomeStatisticsDto> GetHomeStatisticsAsync();
    }
}
