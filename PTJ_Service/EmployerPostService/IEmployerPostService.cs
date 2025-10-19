using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO;
using EmployerPostModel = PTJ_Models.Models.EmployerPost;

namespace PTJ_Service.EmployerPostService
{
    public interface IEmployerPostService
    {
        Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostDto dto);

        Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync();
        Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId);
        Task<EmployerPostDtoOut?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
