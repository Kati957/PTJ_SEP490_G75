using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
        public class AdminJobPostRepository : IAdminJobPostRepository
        {
            private readonly JobMatchingDbContext _db;
            public AdminJobPostRepository(JobMatchingDbContext db) => _db = db;

            //  Employer Posts 

            public async Task<IEnumerable<AdminEmployerPostDto>> GetEmployerPostsAsync(string status = null, int? categoryId = null, string keyword = null)
            {
                var q = _db.EmployerPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Include(p => p.User.EmployerProfile)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);
                if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLower();
                    q = q.Where(p =>
                        p.Title.ToLower().Contains(kw) ||
                        (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                        (p.Location != null && p.Location.ToLower().Contains(kw)) ||
                        p.User.Email.ToLower().Contains(kw));
                }

                var list = await q
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new AdminEmployerPostDto
                    {
                        Id = p.EmployerPostId,
                        Title = p.Title,
                        EmployerUserId = p.UserId,
                        EmployerEmail = p.User.Email,
                        EmployerName = p.User.EmployerProfile != null ? p.User.EmployerProfile.DisplayName : null,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return list;
            }

            public async Task<AdminEmployerPostDetailDto> GetEmployerPostDetailAsync(int id)
            {
                return await _db.EmployerPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Include(p => p.User.EmployerProfile)
                    .Where(p => p.EmployerPostId == id)
                    .Select(p => new AdminEmployerPostDetailDto
                    {
                        Id = p.EmployerPostId,
                        Title = p.Title,
                        Description = p.Description,
                        Salary = p.Salary,
                        Requirements = p.Requirements,
                        WorkHours = p.WorkHours,
                        Location = p.Location,
                        PhoneContact = p.PhoneContact,
                        EmployerUserId = p.UserId,
                        EmployerEmail = p.User.Email,
                        EmployerName = p.User.EmployerProfile != null ? p.User.EmployerProfile.DisplayName : null,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }

            public async Task<bool> ToggleEmployerPostBlockedAsync(int id)
            {
                var post = await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);
                if (post == null) return false;

                post.Status = post.Status == "Blocked" ? "Active" : "Blocked";
                post.UpdatedAt = System.DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }

            public Task<EmployerPost> GetEmployerPostEntityAsync(int id)
                => _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);

            // ================= JobSeeker Posts =================

            public async Task<IEnumerable<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string status = null, int? categoryId = null, string keyword = null)
            {
                var q = _db.JobSeekerPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Include(p => p.User.JobSeekerProfile)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);
                if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLower();
                    q = q.Where(p =>
                        p.Title.ToLower().Contains(kw) ||
                        (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                        (p.PreferredLocation != null && p.PreferredLocation.ToLower().Contains(kw)) ||
                        p.User.Email.ToLower().Contains(kw));
                }

                var list = await q
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new AdminJobSeekerPostDto
                    {
                        Id = p.JobSeekerPostId,
                        Title = p.Title,
                        UserId = p.UserId,
                        UserEmail = p.User.Email,
                        FullName = p.User.JobSeekerProfile != null ? p.User.JobSeekerProfile.FullName : null,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Gender = p.Gender,
                        PreferredLocation = p.PreferredLocation,
                        PreferredWorkHours = p.PreferredWorkHours,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return list;
            }

            public async Task<AdminJobSeekerPostDetailDto> GetJobSeekerPostDetailAsync(int id)
            {
                return await _db.JobSeekerPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Include(p => p.User.JobSeekerProfile)
                    .Where(p => p.JobSeekerPostId == id)
                    .Select(p => new AdminJobSeekerPostDetailDto
                    {
                        Id = p.JobSeekerPostId,
                        Title = p.Title,
                        Description = p.Description,
                        UserId = p.UserId,
                        UserEmail = p.User.Email,
                        FullName = p.User.JobSeekerProfile != null ? p.User.JobSeekerProfile.FullName : null,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Gender = p.Gender,
                        PreferredLocation = p.PreferredLocation,
                        PreferredWorkHours = p.PreferredWorkHours,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }

            public async Task<bool> ToggleJobSeekerPostArchivedAsync(int id)
            {
                var post = await _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
                if (post == null) return false;

                post.Status = post.Status == "Archived" ? "Active" : "Archived";
                post.UpdatedAt = System.DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }

            public Task<JobSeekerPost> GetJobSeekerPostEntityAsync(int id)
                => _db.JobSeekerPosts.FirstOrDefaultAsync(p => p.JobSeekerPostId == id);

            public Task SaveChangesAsync() => _db.SaveChangesAsync();
        }
    }