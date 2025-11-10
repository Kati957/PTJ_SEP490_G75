using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.Report;
using PTJ_Models.Models;
using PTJ_Service.Interfaces;

namespace PTJ_Service.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repo;
        public ReportService(IReportRepository repo) => _repo = repo;

        public async Task<int> ReportEmployerPostAsync(int reporterId, CreateEmployerPostReportDto dto)
        {
            if (dto.EmployerPostId <= 0) throw new ArgumentException("Invalid EmployerPostId");

            // Validate tồn tại
            if (!await _repo.EmployerPostExistsAsync(dto.EmployerPostId))
                throw new KeyNotFoundException("Employer post not found.");

            // Chống spam report lặp trong 10 phút
            if (await _repo.HasRecentDuplicateAsync(reporterId, "EmployerPost", dto.EmployerPostId, withinMinutes: 10))
                throw new InvalidOperationException("You already reported this post recently.");

            var report = new PostReport
            {
                ReporterId = reporterId,
                ReportType = "EmployerPost",
                ReportedItemId = dto.EmployerPostId,
                EmployerPostId = dto.EmployerPostId,
                JobSeekerPostId = null,
                TargetUserId = null,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            await _repo.SaveChangesAsync();
            return report.PostReportId;
        }

        public async Task<int> ReportJobSeekerPostAsync(int reporterId, CreateJobSeekerPostReportDto dto)
        {
            if (dto.JobSeekerPostId <= 0) throw new ArgumentException("Invalid JobSeekerPostId");

            if (!await _repo.JobSeekerPostExistsAsync(dto.JobSeekerPostId))
                throw new KeyNotFoundException("Job seeker post not found.");

            if (await _repo.HasRecentDuplicateAsync(reporterId, "JobSeekerPost", dto.JobSeekerPostId, withinMinutes: 10))
                throw new InvalidOperationException("You already reported this post recently.");

            var report = new PostReport
            {
                ReporterId = reporterId,
                ReportType = "JobSeekerPost",
                ReportedItemId = dto.JobSeekerPostId,
                EmployerPostId = null,
                JobSeekerPostId = dto.JobSeekerPostId,
                TargetUserId = null,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(report);
            await _repo.SaveChangesAsync();
            return report.PostReportId;
        }

        public Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId)
            => _repo.GetMyReportsAsync(reporterId);
    }
}
