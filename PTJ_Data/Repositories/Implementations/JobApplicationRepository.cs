using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
using PTJ_Models.DTO.ApplicationDTO;
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

        public async Task AddAsync(JobSeekerSubmission entity)
            {
            _db.JobSeekerSubmissions.Add(entity);
            await _db.SaveChangesAsync();
            }

        public async Task<bool> ExistsAsync(int jobSeekerId, int employerPostId)
            {
            return await _db.JobSeekerSubmissions
                .AnyAsync(x => x.JobSeekerId == jobSeekerId && x.EmployerPostId == employerPostId);
            }

        public async Task<JobSeekerSubmission?> GetAsync(int jobSeekerId, int employerPostId)
            {
            return await _db.JobSeekerSubmissions
                .FirstOrDefaultAsync(x => x.JobSeekerId == jobSeekerId && x.EmployerPostId == employerPostId);
            }

        public async Task<JobSeekerSubmission?> GetByIdAsync(int id)
            {
            return await _db.JobSeekerSubmissions.FindAsync(id);
            }

        public async Task<IEnumerable<JobSeekerSubmission>> GetByEmployerPostWithDetailAsync(int employerPostId)
            {
            return await _db.JobSeekerSubmissions
                .Include(x => x.Cv)                
                .Include(x => x.JobSeeker)
                .Where(x => x.EmployerPostId == employerPostId)
                .OrderByDescending(x => x.AppliedAt)
                .ToListAsync();
            }


        public async Task<IEnumerable<JobSeekerSubmission>> GetByJobSeekerWithPostDetailAsync(int jobSeekerId)
            {
            return await _db.JobSeekerSubmissions
                .Include(x => x.Cv)
                .Include(x => x.JobSeeker)               
                .Include(x => x.EmployerPost)
                    .ThenInclude(p => p.User)
                .Include(x => x.EmployerPost.Category)
                .Where(x => x.JobSeekerId == jobSeekerId)
                .OrderByDescending(x => x.AppliedAt)
                .ToListAsync();
            }

        public async Task<ApplicationSummaryDto> GetFullSummaryAsync(int? employerId)
        {
           
            var baseQuery = _db.JobSeekerSubmissions
                .Include(x => x.JobSeeker)
                .Include(x => x.EmployerPost)
                .AsQueryable();

            if (employerId.HasValue)
                baseQuery = baseQuery.Where(x => x.EmployerPost.UserId == employerId.Value);
            var pendingQuery = baseQuery
                .Where(x => x.Status == "Pending")
                .OrderByDescending(x => x.AppliedAt)
                .Select(x => new ApplicationSimpleDto
                {
                    SubmissionId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker.Username,
                    PostId = x.EmployerPostId,
                    PostTitle = x.EmployerPost.Title,
                    Status = x.Status,
                    AppliedAt = x.AppliedAt
                });

            var pending = await pendingQuery.ToListAsync();
            var reviewedQuery = baseQuery
                .Where(x => x.Status == "Accepted"
                         || x.Status == "Rejected"
                         || x.Status == "Interviewing")
                .OrderByDescending(x => x.AppliedAt)
                .Select(x => new ApplicationSimpleDto
                {
                    SubmissionId = x.SubmissionId,
                    JobSeekerId = x.JobSeekerId,
                    Username = x.JobSeeker.Username,
                    PostId = x.EmployerPostId,
                    PostTitle = x.EmployerPost.Title,
                    Status = x.Status,
                    AppliedAt = x.AppliedAt
                });

            var reviewed = await reviewedQuery.ToListAsync();
            return new ApplicationSummaryDto
            {
                PendingTotal = pending.Count,
                ReviewedTotal = reviewed.Count,
                PendingApplications = pending,
                ReviewedApplications = reviewed
            };
        }



        public async Task UpdateAsync(JobSeekerSubmission entity)
            {
            _db.JobSeekerSubmissions.Update(entity);
            await _db.SaveChangesAsync();
            }

        public async Task SaveChangesAsync()
            {
            await _db.SaveChangesAsync();
            }
        }
    }
