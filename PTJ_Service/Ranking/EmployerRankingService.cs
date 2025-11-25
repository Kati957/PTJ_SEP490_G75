using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Employer;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class EmployerRankingService : IEmployerRankingService
    {
        private readonly IEmployerRankingRepository _repo;

        public EmployerRankingService(IEmployerRankingRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<EmployerRankingDto>> GetTopEmployersAsync(int top = 10)
        {
            if (top <= 0) top = 10;
            return await _repo.GetTopEmployersByApplyCountAsync(top);
        }
    }
}
