using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTJ_Data;
using System.Security.Claims;

namespace PTJ_API.Attributes
{
    public class RequireVerifiedEmailAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kiểm tra có đăng nhập chưa
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Unauthorized" });
                return;
            }

            // Lấy claim IsVerified từ token (nếu có)
            var verifiedClaim = user.FindFirst("IsVerified")?.Value;

            if (verifiedClaim == "True" || verifiedClaim == "true")
                return; // OK, đã xác thực

            // Nếu token không chứa claim → fallback kiểm tra DB
            var db = context.HttpContext.RequestServices.GetRequiredService<JobMatchingDbContext>();
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdStr, out var userId))
            {
                var dbUser = db.Users.FirstOrDefault(u => u.UserId == userId);
                if (dbUser != null && dbUser.IsVerified)
                    return; // OK
            }

            // ❌ Chưa xác thực email → chặn
            context.Result = new ObjectResult(new
            {
                message = "Please verify your email before using this feature."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
