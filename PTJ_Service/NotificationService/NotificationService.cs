using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PTJ_Service.Hubs;

namespace PTJ_Service.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly JobMatchingOpenAiDbContext _db;

        public NotificationService(
            INotificationRepository repo,
            IHubContext<NotificationHub> hub,
            JobMatchingOpenAiDbContext db)
        {
            _repo = repo;
            _hub = hub;
            _db = db;
        }

        public async Task SendAsync(CreateNotificationDto dto)
        {
            // 1. Load template
            var template = await _repo.GetTemplateAsync(dto.NotificationType);
            if (template == null)
                throw new Exception($"Không tìm thấy mẫu thông báo: {dto.NotificationType}");

            // 2. Inject auto values 
            await InjectRealNamesIntoData(dto, template);

            // 3. Render title & message
            var title = RenderTemplate(template.TitleTemplate, dto.Data);
            var message = RenderTemplate(template.MessageTemplate, dto.Data);

            // 4. Create Notification
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

            await _repo.CreateAsync(noti);

            // 5. Push SignalR
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

        //SUPPORT FUNCTIONS – SMART AUTO INJECT

        private async Task InjectRealNamesIntoData(CreateNotificationDto dto, NotificationTemplate template)
        {
           
            if ((template.TitleTemplate.Contains("{{Name}}") ||
                 template.MessageTemplate.Contains("{{Name}}"))
                 && !dto.Data.ContainsKey("Name"))
            {
                dto.Data["Name"] = await GetRealName(dto.UserId);
            }
            if ((template.TitleTemplate.Contains("{{JobSeekerName}}") ||
                 template.MessageTemplate.Contains("{{JobSeekerName}}"))
                 && !dto.Data.ContainsKey("JobSeekerName"))
            {
                dto.Data["JobSeekerName"] = "Ứng viên";
            }
            if ((template.TitleTemplate.Contains("{{EmployerName}}") ||
                 template.MessageTemplate.Contains("{{EmployerName}}"))
                 && !dto.Data.ContainsKey("EmployerName"))
            {
                dto.Data["EmployerName"] = "Nhà tuyển dụng";
            }
        }
        private async Task<string> GetRealName(int userId)
        {
            var js = await _db.JobSeekerProfiles
                .Where(x => x.UserId == userId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync();

            if (js != null) return js;

            var emp = await _db.EmployerProfiles
                .Where(x => x.UserId == userId)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync();

            if (emp != null) return emp;

            return await _db.Users
                .Where(x => x.UserId == userId)
                .Select(x => x.Username)
                .FirstOrDefaultAsync() ?? "";
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
