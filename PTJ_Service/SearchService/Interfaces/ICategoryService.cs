using PTJ_Models;
using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.SearchService.Interfaces
    {
    public interface ICategoryService
        {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        }
    }
