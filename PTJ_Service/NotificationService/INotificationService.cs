using PTJ_Models.DTO.Notification;

namespace PTJ_Service.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(CreateNotificationDto dto);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool? isRead = null);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
    }
}
