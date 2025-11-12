using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Interfaces.ActivityUsers
{
    public interface IUserActivityRepository
    {
        Task AddAsync(UserActivityLog log);
        Task<IEnumerable<UserActivityLog>> GetByUserAsync(int userId, DateTime? from = null, DateTime? to = null);
    }
}
