using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly JobMatchingDbContext _db;

        public NotificationRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<NotificationTemplate?> GetTemplateAsync(string type)
        {
            return await _db.NotificationTemplates
                .FirstOrDefaultAsync(t => t.NotificationType == type);
        }

        public async Task<int> CreateAsync(Notification entity)
        {
            _db.Notifications.Add(entity);
            await _db.SaveChangesAsync();
            return entity.NotificationId;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool? isRead = null)
        {
            var query = _db.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .AsQueryable();

            if (isRead.HasValue)
                query = query.Where(x => x.IsRead == isRead.Value);

            return await query.ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var noti = await _db.Notifications
                .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);

            if (noti == null) return false;

            noti.IsRead = true;
            noti.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
