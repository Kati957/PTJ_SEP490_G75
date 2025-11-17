using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Service.Interfaces;
using System.Security.Claims;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _svc;

        public NotificationController(INotificationService svc)
        {
            _svc = svc;
        }

        private int UserId =>
            int.Parse(User.FindFirstValue("id")!);

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead = null)
        {
            var result = await _svc.GetUserNotificationsAsync(UserId, isRead);
            return Ok(result);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _svc.MarkAsReadAsync(id, UserId);
            return Ok(new { success });
        }
    }
}
