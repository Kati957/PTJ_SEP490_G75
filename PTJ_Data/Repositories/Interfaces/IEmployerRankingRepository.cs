using PTJ_Models.DTO.Employer;

namespace PTJ_Data.Repositories.Interfaces
{
    public interface IEmployerRankingRepository
    {
        Task<List<EmployerRankingDto>> GetTopEmployersByApplyCountAsync(int top);
    }
}
