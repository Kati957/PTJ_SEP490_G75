using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;
using Microsoft.AspNetCore.SignalR;
using PTJ_Service.Hubs;

namespace PTJ_Service.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(
            INotificationRepository repo,
            IHubContext<NotificationHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }

        public async Task SendAsync(CreateNotificationDto dto)
        {
            // 1. Load Notification Template
            var template = await _repo.GetTemplateAsync(dto.NotificationType);
            if (template == null)
                throw new Exception($"Notification template not found: {dto.NotificationType}");

            // 2. Render Title & Message (replace placeholders)
            string title = RenderTemplate(template.TitleTemplate, dto.Data);
            string message = RenderTemplate(template.MessageTemplate, dto.Data);

            // 3. Create Notification Entity
            var noti = new Notification
            {
                UserId = dto.UserId,
                NotificationType = dto.NotificationType,
                RelatedItemId = dto.RelatedItemId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Save to DB
            await _repo.CreateAsync(noti);

            // 4. PUSH REAL-TIME SIGNALR 🔥
            await _hub.Clients.Group($"user_{dto.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    NotificationId = noti.NotificationId,
                    Type = noti.NotificationType,
                    Title = noti.Title,
                    Message = noti.Message,
                    RelatedItemId = noti.RelatedItemId,
                    CreatedAt = noti.CreatedAt
                });
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool? isRead = null)
        {
            var data = await _repo.GetUserNotificationsAsync(userId, isRead);

            return data.Select(x => new NotificationDto
            {
                NotificationId = x.NotificationId,
                NotificationType = x.NotificationType,
                RelatedItemId = x.RelatedItemId,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            });
        }

        public Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            return _repo.MarkAsReadAsync(notificationId, userId);
        }

        private string RenderTemplate(string template, Dictionary<string, string> data)
        {
            foreach (var item in data)
            {
                template = template.Replace("{{" + item.Key + "}}", item.Value);
            }
            return template;
        }
    }
}
