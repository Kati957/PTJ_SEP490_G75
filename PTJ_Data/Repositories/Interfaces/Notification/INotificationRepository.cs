using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<int> CreateAsync(Notification entity);
        Task<NotificationTemplate?> GetTemplateAsync(string type);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool? isRead = null);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
    }
}
