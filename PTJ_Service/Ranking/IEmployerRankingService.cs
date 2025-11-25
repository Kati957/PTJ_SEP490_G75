using PTJ_Models.DTO.Employer;

namespace PTJ_Service.Interfaces
{
    public interface IEmployerRankingService
    {
        Task<List<EmployerRankingDto>> GetTopEmployersAsync(int top = 10);
    }
}
