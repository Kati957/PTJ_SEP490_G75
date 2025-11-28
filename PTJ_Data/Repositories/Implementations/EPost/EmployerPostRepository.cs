using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Models;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.EPost
{
    public class EmployerPostRepository : IEmployerPostRepository
    {
        private readonly JobMatchingDbContext _db;

        public EmployerPostRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<EmployerPost>> GetAllAsync()
        {
            return await _db.EmployerPosts
                .Include(p => p.User).ThenInclude(p => p.EmployerProfile)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active" && p.User.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployerPost>> GetByUserAsync(int userId)
        {
            return await _db.EmployerPosts
                .Include(p => p.User).ThenInclude(p => p.EmployerProfile)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && p.Status != "Deleted")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<EmployerPost?> GetByIdAsync(int id)
        {
            return await _db.EmployerPosts
                .Include(p => p.User).ThenInclude(p => p.EmployerProfile)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);
        }

        public async Task AddAsync(EmployerPost post)
        {
            _db.EmployerPosts.Add(post);
        }

        public async Task UpdateAsync(EmployerPost post)
        {
            _db.EmployerPosts.Update(post);
        }

        public async Task SoftDeleteAsync(int id)
        {
            var post = await _db.EmployerPosts.FindAsync(id);
            if (post != null)
            {
                post.Status = "Deleted";
                post.UpdatedAt = DateTime.Now;
            }
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

    }
}