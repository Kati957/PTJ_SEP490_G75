using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Implementations
    {
    public class JobApplicationRepository : IJobApplicationRepository
        {
        private readonly JobMatchingDbContext _db;

        public JobApplicationRepository(JobMatchingDbContext db)
            {
            _db = db;
            }

        public async Task AddAsync(EmployerCandidatesList entity)
            {
            _db.EmployerCandidatesLists.Add(entity);
            await _db.SaveChangesAsync();
            }

        public async Task<bool> ExistsAsync(int jobSeekerId, int employerPostId)
            {
            return await _db.EmployerCandidatesLists
                .AnyAsync(x => x.JobSeekerId == jobSeekerId && x.EmployerPostId == employerPostId);
            }

        public async Task<EmployerCandidatesList?> GetAsync(int jobSeekerId, int employerPostId)
            {
            return await _db.EmployerCandidatesLists
                .FirstOrDefaultAsync(x => x.JobSeekerId == jobSeekerId && x.EmployerPostId == employerPostId);
            }

        public async Task<EmployerCandidatesList?> GetByIdAsync(int id)
            {
            return await _db.EmployerCandidatesLists.FindAsync(id);
            }

        public async Task<IEnumerable<EmployerCandidatesList>> GetByEmployerPostWithDetailAsync(int employerPostId)
            {
            return await _db.EmployerCandidatesLists
                .Include(x => x.JobSeeker)
                    .ThenInclude(u => u.JobSeekerProfile) // 🟢 load thêm profile
                .Include(x => x.JobSeeker)
                    .ThenInclude(u => u.JobSeekerPosts)
                        .ThenInclude(p => p.Category)
                .Where(x => x.EmployerPostId == employerPostId)
                .OrderByDescending(x => x.ApplicationDate)
                .ToListAsync();
            }



        public async Task<IEnumerable<EmployerCandidatesList>> GetByJobSeekerWithPostDetailAsync(int jobSeekerId)
            {
            return await _db.EmployerCandidatesLists
                .Include(x => x.EmployerPost)
                .ThenInclude(p => p.User)           // lấy thông tin employer
                .Include(x => x.EmployerPost.Category) // lấy category của bài đăng
                .Where(x => x.JobSeekerId == jobSeekerId)
                .OrderByDescending(x => x.ApplicationDate)
                .ToListAsync();
            }


        public async Task UpdateAsync(EmployerCandidatesList entity)
            {
            _db.EmployerCandidatesLists.Update(entity);
            await _db.SaveChangesAsync();
            }

        public async Task SaveChangesAsync()
            {
            await _db.SaveChangesAsync();
            }
        }
    }

