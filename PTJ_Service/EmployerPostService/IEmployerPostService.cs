using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO;
using EmployerPostModel = PTJ_Models.Models.EmployerPost;

namespace PTJ_Service.EmployerPostService
{
    public interface IEmployerPostService
    {
        Task<EmployerPostModel> CreateEmployerPostAsync(EmployerPostDto dto);
        Task<IEnumerable<EmployerPostModel>> GetAllAsync();
        Task<EmployerPostModel?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
