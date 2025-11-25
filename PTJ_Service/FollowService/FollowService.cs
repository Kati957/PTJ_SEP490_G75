using PTJ_Data;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using Microsoft.EntityFrameworkCore;
using PTJ_Models;
using PTJ_Service.FollowService;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Notification;

namespace PTJ_Service.FollowService
{
    public class FollowService : IFollowService
    {
        private readonly JobMatchingDbContext _context;
        private readonly INotificationService _noti;

        public FollowService(JobMatchingDbContext context, INotificationService noti)
        {
            _context = context;
            _noti = noti;
        }

        public async Task<bool> FollowEmployerAsync(int jobSeekerId, int employerId)
        {
            var existing = await _context.EmployerFollowers
                .FirstOrDefaultAsync(f => f.JobSeekerId == jobSeekerId && f.EmployerId == employerId);

            // true nếu là follow mới hoặc re-follow sau khi từng unfollow
            bool isNewFollow = existing == null || existing.IsActive == false;

            if (existing != null)
            {
                existing.IsActive = true;
                existing.FollowDate = DateTime.Now;
            }
            else
            {
                _context.EmployerFollowers.Add(new EmployerFollower
                {
                    JobSeekerId = jobSeekerId,
                    EmployerId = employerId,
                    IsActive = true,
                    FollowDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            //  Gửi noti cho Employer khi có follower mới
            if (isNewFollow)
            {
                var seeker = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == jobSeekerId);

                if (seeker != null)
                {
                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = employerId, // employer là userId trong bảng Users
                        NotificationType = "JobSeekerFollowedEmployer",
                        RelatedItemId = jobSeekerId,
                        Data = new()
                        {
                            { "JobSeekerName", seeker.Username }
                        }
                    });
                }
            }

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
