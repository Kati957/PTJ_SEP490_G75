using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        public NotificationService(INotificationRepository repo) => _repo = repo;

        public async Task SendNotificationAsync(
            int userId,
            string title,
            string message,
            string type,
            int? relatedId = null)
        {
            var noti = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = type,
                RelatedItemId = relatedId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(noti);
            await _repo.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            var notis = await _repo.GetByUserIdAsync(userId);

            return notis.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                NotificationType = n.NotificationType,
                RelatedItemId = n.RelatedItemId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var noti = await _repo.GetByIdAsync(notificationId);
            if (noti == null) return;

            noti.IsRead = true;
            noti.UpdatedAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync();
        }
    }
}
