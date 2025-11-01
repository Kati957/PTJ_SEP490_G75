using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations
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
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            }

        public async Task<IEnumerable<EmployerPost>> GetByUserAsync(int userId)
            {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            }

        public async Task<EmployerPost?> GetByIdAsync(int id)
            {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);
            }

        public async Task AddAsync(EmployerPost post)
            {
            _db.EmployerPosts.Add(post);
            await _db.SaveChangesAsync();
            }

        public async Task UpdateAsync(EmployerPost post)
            {
            _db.EmployerPosts.Update(post);
            await _db.SaveChangesAsync();
            }

        public async Task SoftDeleteAsync(int id)
            {
            var post = await _db.EmployerPosts.FindAsync(id);
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
