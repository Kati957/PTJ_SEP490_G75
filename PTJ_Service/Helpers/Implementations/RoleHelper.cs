using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;
using PTJ_Models.Models;

namespace PTJ_Service.Helpers.Implementations
{
    public static class RoleHelper
    {

        // Xóa toàn bộ role hiện có và gán DUY NHẤT roleName cho user (dùng skip navigation).

        public static async Task SetSingleRoleAsync(JobMatchingDbContext db, int userId, string roleName)
        {
            roleName = roleName.Trim();

            var user = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new Exception($"User #{userId} not found.");

            var role = await db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);

            if (role == null)
                throw new Exception($"Role '{roleName}' not found. Seed Roles table first.");

            user.Roles.Clear();
            user.Roles.Add(role);

            await db.SaveChangesAsync();
        }

        public static async Task EnsureRoleIfMissingAsync(JobMatchingDbContext db, int userId, string roleName)
        {
            roleName = roleName.Trim();

            var user = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new Exception($"User #{userId} not found.");

            if (user.Roles.Count == 0)
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null)
                    throw new Exception($"Role '{roleName}' not found. Seed Roles table first.");

                user.Roles.Add(role);
                await db.SaveChangesAsync();
            }
        }
    }
}
