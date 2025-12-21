using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using System.Security.Claims;

namespace PTJ_API.Middlewares
{
    public class AccountDisabledMiddleware
    {
        private readonly RequestDelegate _next;

        public AccountDisabledMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, JobMatchingOpenAiDbContext db)
        {
            // Chưa login → cho qua
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                await _next(context);
                return;
            }

            // Lấy UserId từ token
            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                await _next(context);
                return;
            }

            // Kiểm tra trạng thái tài khoản
            var isActive = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.IsActive)
                .FirstOrDefaultAsync();

            if (!isActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "ACCOUNT_DISABLED",
                    message = "Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa."
                });
                return;
            }

            await _next(context);
        }
    }
}
