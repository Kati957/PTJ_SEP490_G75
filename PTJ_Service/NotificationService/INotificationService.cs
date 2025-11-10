using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Notification;

namespace PTJ_Service.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(
            int userId,
            string title,
            string message,
            string type,
            int? relatedId = null);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
    }
}
