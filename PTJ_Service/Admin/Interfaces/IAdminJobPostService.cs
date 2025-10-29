using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
    public interface IAdminJobPostService
    {
        // Employer
        Task<IEnumerable<AdminEmployerPostDto>> GetEmployerPostsAsync(string status = null, int? categoryId = null, string keyword = null);
        Task<AdminEmployerPostDetailDto> GetEmployerPostDetailAsync(int id);
        Task ToggleEmployerPostBlockedAsync(int id);

        // JobSeeker
        Task<IEnumerable<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(string status = null, int? categoryId = null, string keyword = null);
        Task<AdminJobSeekerPostDetailDto> GetJobSeekerPostDetailAsync(int id);
        Task ToggleJobSeekerPostArchivedAsync(int id);
    }
}
