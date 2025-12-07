using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PTJ_API.Hubs
    {
    // Dùng Authorize để chỉ user đã login mới connect được
    [Authorize]
    public class PaymentHub : Hub
        {
        // Tạm thời chưa cần method gì, chỉ dùng để server push về
        }
    }
