using PTJ_Data;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Service.FollowService.Implementations
{
    public class FollowService : IFollowService
    {
        private readonly JobMatchingDbContext _context;

        public FollowService(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> FollowEmployerAsync(int jobSeekerId, int employerId)
        {
            var existing = await _context.EmployerFollowers
                .FirstOrDefaultAsync(f => f.JobSeekerId == jobSeekerId && f.EmployerId == employerId);

            if (existing != null)
            {
                existing.IsActive = true;
                existing.FollowDate = DateTime.UtcNow;
            }
            else
            {
                await _context.EmployerFollowers.AddAsync(new EmployerFollower
                {
                    JobSeekerId = jobSeekerId,
                    EmployerId = employerId,
                    IsActive = true,
                    FollowDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowEmployerAsync(int jobSeekerId, int employerId)
        {
            var existing = await _context.EmployerFollowers
                .FirstOrDefaultAsync(f => f.JobSeekerId == jobSeekerId && f.EmployerId == employerId);

            if (existing == null) return false;

            existing.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(int jobSeekerId, int employerId)
        {
            return await _context.EmployerFollowers
                .AnyAsync(f => f.JobSeekerId == jobSeekerId && f.EmployerId == employerId && f.IsActive);
        }

        public async Task<IEnumerable<EmployerFollowDto>> GetFollowingListAsync(int jobSeekerId)
        {
            return await _context.EmployerFollowers
                .Where(f => f.JobSeekerId == jobSeekerId && f.IsActive)
                .Include(f => f.Employer)
                .Select(f => new EmployerFollowDto
                {
                    EmployerID = f.EmployerId,
                    EmployerName = f.Employer.Username,
                    FollowDate = f.FollowDate
                })
                .ToListAsync();
        }
    }
}
