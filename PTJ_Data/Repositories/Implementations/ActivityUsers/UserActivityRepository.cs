using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;
using PTJ_Models;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.ActivityUsers;

namespace PTJ_Data.Repositories.Implementations.ActivityUsers

{
    public class UserActivityRepository : IUserActivityRepository
    {
        private readonly JobMatchingOpenAiDbContext _context;

        public UserActivityRepository(JobMatchingOpenAiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserActivityLog log)
        {
            await _context.UserActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserActivityLog>> GetByUserAsync(int userId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.UserActivityLogs
                .Where(l => l.UserId == userId);

            if (from.HasValue && to.HasValue)
                query = query.Where(l => l.Timestamp >= from && l.Timestamp <= to);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
