using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class JobSeekerPostRepository : IJobSeekerPostRepository
        {
        private readonly JobMatchingDbContext _db;

        public JobSeekerPostRepository(JobMatchingDbContext db)
            {
            _db = db;
            }

        public async Task<IEnumerable<JobSeekerPost>> GetAllAsync()
            {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            }

        public async Task<IEnumerable<JobSeekerPost>> GetByUserAsync(int userId)
            {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            }

        public async Task<JobSeekerPost?> GetByIdAsync(int id)
            {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
            }

        public async Task AddAsync(JobSeekerPost post)
            {
            _db.JobSeekerPosts.Add(post);
            await _db.SaveChangesAsync();
            }

        public async Task UpdateAsync(JobSeekerPost post)
            {
            _db.JobSeekerPosts.Update(post);
            await _db.SaveChangesAsync();
            }

        public async Task SoftDeleteAsync(int id)
            {
            var post = await _db.JobSeekerPosts.FindAsync(id);
            if (post != null)
                {
                post.Status = "Deleted";
                post.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                }
            }

        public async Task SaveChangesAsync()
            {
            await _db.SaveChangesAsync();
            }
        }
    }
