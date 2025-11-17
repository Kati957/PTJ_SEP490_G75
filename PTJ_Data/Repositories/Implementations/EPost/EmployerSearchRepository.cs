using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Models;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.DTO.SearchDTO;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.EPost
{
    public class EmployerSearchRepository : IEmployerSearchRepository
    {
        private readonly JobMatchingDbContext _db;

        public EmployerSearchRepository(JobMatchingDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> SearchJobSeekersAsync(EmployerSearchFilterDto filter)
        {
            var query = _db.JobSeekerPosts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Where(p => p.Status == "Active")
                .AsQueryable();

            //  Keyword
            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                var key = filter.Keyword.ToLower();
                query = query.Where(p =>
                    p.Title != null && p.Title.ToLower().Contains(key) ||
                    p.Description != null && p.Description.ToLower().Contains(key));
            }

            //  Category
            if (filter.CategoryID.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryID.Value);

            //  Location
            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(p => p.PreferredLocation.Contains(filter.Location));

            //  Job Type
            if (!string.IsNullOrEmpty(filter.PreferredJobType))
                query = query.Where(p => p.PreferredWorkHours.Contains(filter.PreferredJobType));

            //  Map sang DTO
            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = p.JobSeekerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Age = p.Age,
                    Gender = p.Gender,
                    PreferredWorkHours = p.PreferredWorkHours,
                    PreferredLocation = p.PreferredLocation,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    SeekerName = p.User != null ? p.User.Username : null,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .ToListAsync();
        }
    }
}
