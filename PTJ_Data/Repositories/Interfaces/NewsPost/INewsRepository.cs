using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.NewsPost
{
    public interface INewsRepository
    {
        Task<(List<News> Data, int Total)> GetPagedAsync(
            string? keyword, string? category, int page, int pageSize, string sortBy, bool desc);

        Task<News?> GetByIdAsync(int id);
    }
}
