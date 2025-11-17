using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Implementations
    {
    public class JobSeekerCvRepository : IJobSeekerCvRepository
        {
        private readonly JobMatchingDbContext _db;

        public JobSeekerCvRepository(JobMatchingDbContext db)
            {
            _db = db;
            }

        public async Task<JobSeekerCv?> GetByIdAsync(int id)
            {
            return await _db.JobSeekerCvs
                .Include(c => c.JobSeeker)
                .FirstOrDefaultAsync(c => c.Cvid == id);
            }

        public async Task<IEnumerable<JobSeekerCv>> GetByJobSeekerAsync(int jobSeekerId)
            {
            return await _db.JobSeekerCvs
                .Where(c => c.JobSeekerId == jobSeekerId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
            }

        public async Task AddAsync(JobSeekerCv entity)
            {
            await _db.JobSeekerCvs.AddAsync(entity);
            await _db.SaveChangesAsync();
            }

        public async Task UpdateAsync(JobSeekerCv entity)
            {
            _db.JobSeekerCvs.Update(entity);
            await _db.SaveChangesAsync();
            }

        public async Task DeleteAsync(JobSeekerCv entity)
            {
            _db.JobSeekerCvs.Remove(entity);
            await _db.SaveChangesAsync();
            }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
        }
    }
