using System.Security.Claims;

namespace PTJ_API.Middlewares
{
    public class EmailVerificationMiddleware
    {
        private readonly RequestDelegate _next;

        public EmailVerificationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Nếu chưa đăng nhập → cho qua
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            // Lấy giá trị IsVerified từ JWT
            var verifiedClaim = context.User.Claims
                .FirstOrDefault(c => c.Type == "verified" || c.Type == "IsVerified")
                ?.Value;

            bool isVerified = verifiedClaim?.ToLower() == "true";

            // Nếu đã verify → cho qua
            if (isVerified)
            {
                await _next(context);
                return;
            }

            // Các API cho phép khi chưa xác minh email
            var path = context.Request.Path.Value?.ToLower() ?? "";

            string[] allowedPaths =
            {
                "/api/auth/login",
                "/api/auth/logout",
                "/api/auth/me",
                "/api/auth/verify-email",
                "/api/auth/resend-verification",
                "/api/auth/google/prepare",
                "/api/auth/google/complete"
            };

            if (allowedPaths.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            // Chặn user chưa verify email
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Vui lòng xác minh email trước khi tiếp tục sử dụng hệ thống."
            });

            return;
        }
    }
}
