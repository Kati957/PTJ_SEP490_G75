using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PTJ_API.Hubs
    {
    [Authorize]
    public class PaymentHub : Hub
        {
        public override Task OnConnectedAsync()
            {
            Console.WriteLine($"Client connected: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
            }
        }
    }
