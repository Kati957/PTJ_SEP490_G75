using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PTJ_API.Hubs
    {
    // Dùng Authorize để chỉ user đã login mới connect được
    using System.Security.Claims;
    using Microsoft.AspNetCore.SignalR;

    public class SignalRUserIdProvider : IUserIdProvider
        {
        public string? GetUserId(HubConnectionContext connection)
            {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }

    }
