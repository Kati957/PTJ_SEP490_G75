using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminReportRepository
    {
        //  Lấy danh sách report
        Task<IEnumerable<AdminReportDto>> GetPendingReportsAsync(string? reportType = null, string? keyword = null);
        Task<IEnumerable<AdminSolvedReportDto>> GetSolvedReportsAsync(string? adminKeyword = null);

        //  Chi tiết report + liên quan
        Task<PostReport?> GetReportByIdAsync(int reportId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<EmployerPost?> GetEmployerPostByIdAsync(int postId);
        Task<JobSeekerPost?> GetJobSeekerPostByIdAsync(int postId);

        //  Thao tác xử lý
        Task AddSolvedReportAsync(PostReportSolved solvedReport);
        Task SaveChangesAsync();
    }
}
