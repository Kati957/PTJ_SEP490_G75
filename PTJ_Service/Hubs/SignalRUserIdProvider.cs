using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

public class SignalRUserIdProvider : IUserIdProvider
    {
    public string? GetUserId(HubConnectionContext connection)
        {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
