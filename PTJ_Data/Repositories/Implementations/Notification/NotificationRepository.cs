using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly JobMatchingDbContext _db;
        public NotificationRepository(JobMatchingDbContext db) => _db = db;

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public Task<Notification?> GetByIdAsync(int id)
            => _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id);

        public async Task AddAsync(Notification notification)
        {
            await _db.Notifications.AddAsync(notification);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
