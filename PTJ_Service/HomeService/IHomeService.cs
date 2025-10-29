using PTJ_Models.DTO;

namespace PTJ_Service.HomeService
{
    public interface IHomeService
    {
        Task<IEnumerable<HomePostDto>> GetLatestPostsAsync(string? keyword, int page, int pageSize);
    }
}
