using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.Admin
{
    public interface IAdminJobPostRepository
    {
        // Employer posts
        Task<IEnumerable<AdminEmployerPostDto>> GetEmployerPostsAsync(string status = null, int? categoryId = null, string keyword = null);
        Task<AdminEmployerPostDetailDto> GetEmployerPostDetailAsync(int id);
        Task<bool> ToggleEmployerPostBlockedAsync(int id); // Active <-> Blocked

        // JobSeeker posts
        Task<IEnumerable<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string status = null, int? categoryId = null, string keyword = null);
        Task<AdminJobSeekerPostDetailDto> GetJobSeekerPostDetailAsync(int id);
        Task<bool> ToggleJobSeekerPostArchivedAsync(int id); // Active <-> Archived

        // (tuỳ chọn) truy xuất entity gốc nếu cần
        Task<EmployerPost> GetEmployerPostEntityAsync(int id);
        Task<JobSeekerPost> GetJobSeekerPostEntityAsync(int id);

        Task SaveChangesAsync();
    }
}
