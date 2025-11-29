using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.EPost
{
    public interface IEmployerPostRepository
    {
        Task<IEnumerable<EmployerPost>> GetAllAsync();
        Task<IEnumerable<EmployerPost>> GetByUserAsync(int userId);
        Task<EmployerPost?> GetByIdAsync(int id);

        Task AddAsync(EmployerPost post);
        Task UpdateAsync(EmployerPost post);
        Task SoftDeleteAsync(int id);

        Task SaveChangesAsync();
    }
}
