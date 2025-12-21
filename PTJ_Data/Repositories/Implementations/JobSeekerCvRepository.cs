using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Data.Repositories.Implementations
    {
    public class JobSeekerCvRepository : IJobSeekerCvRepository
        {
        private readonly JobMatchingOpenAiDbContext _db;

        public JobSeekerCvRepository(JobMatchingOpenAiDbContext db)
            {
            _db = db;
            }


        // Lấy CV theo ID – chỉ lấy CV chưa bị xóa

        public async Task<JobSeekerCv?> GetByIdAsync(int id)
            {
            var sql = @"
                SELECT * 
                FROM JobSeekerCvs 
                WHERE Cvid = @CvId AND (IsDeleted = 0 OR IsDeleted IS NULL)";

            return await _db.JobSeekerCvs
                .FromSqlRaw(sql, new SqlParameter("@CvId", id))
                .Include(c => c.JobSeeker)
                .FirstOrDefaultAsync();
            }


        // Lấy toàn bộ CV của một ứng viên – chỉ lấy CV chưa xóa

        public async Task<IEnumerable<JobSeekerCv>> GetByJobSeekerAsync(int jobSeekerId)
            {
            var sql = @"
                SELECT * 
                FROM JobSeekerCvs 
                WHERE JobSeekerId = @JobSeekerId AND (IsDeleted = 0 OR IsDeleted IS NULL)
                ORDER BY UpdatedAt DESC";

            return await _db.JobSeekerCvs
                .FromSqlRaw(sql, new SqlParameter("@JobSeekerId", jobSeekerId))
                .ToListAsync();
            }


        // Thêm mới CV

        public async Task AddAsync(JobSeekerCv entity)
            {
            await _db.JobSeekerCvs.AddAsync(entity);
            await _db.SaveChangesAsync();
            }


        // Cập nhật CV

        public async Task UpdateAsync(JobSeekerCv entity)
            {
            _db.JobSeekerCvs.Update(entity);
            await _db.SaveChangesAsync();
            }


        // Soft Delete – không xóa record khỏi DB

        public async Task SoftDeleteAsync(int cvId)
            {
            var sql = "UPDATE JobSeekerCvs SET IsDeleted = 1, UpdatedAt = @Now WHERE Cvid = @CvId";
            await _db.Database.ExecuteSqlRawAsync(sql,
                new SqlParameter("@CvId", cvId),
                new SqlParameter("@Now", DateTime.Now));
            }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
        }
    }
