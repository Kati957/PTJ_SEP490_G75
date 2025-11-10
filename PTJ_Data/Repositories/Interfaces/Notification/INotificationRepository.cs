using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
        Task<Notification?> GetByIdAsync(int id);
        Task AddAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
