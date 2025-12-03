using System.Security.Claims;

namespace PTJ_API.Middlewares
{
    public class PendingEmployerMiddleware
    {
        private readonly RequestDelegate _next;

        public PendingEmployerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Không có user → cho qua
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                await _next(context);
                return;
            }

            // Lấy role
            var roles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isPendingEmployer = roles.Contains("PendingEmployer");

            if (!isPendingEmployer)
            {
                await _next(context);
                return;
            }

            // Những API cho phép PendingEmployer truy cập
            var path = context.Request.Path.Value?.ToLower() ?? "";

            var allowedPaths = new[]
            {
                "/api/auth/me",
                "/api/auth/logout",
                "/api/notifications",          
                "/api/auth/google/complete",   
                "/api/auth/google/prepare",
            };

            if (allowedPaths.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            // Chặn tất cả API khác
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Tài khoản Nhà tuyển dụng của bạn đang chờ phê duyệt từ quản trị viên."
            });

            return;
        }
    }
}
