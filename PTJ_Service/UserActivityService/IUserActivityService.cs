using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Service.UserActivityService
{
    public interface IUserActivityService
    {
        Task LogAsync(int userId, string activityType, string details);
        Task<IEnumerable<UserActivityLog>> GetHistoryAsync(int userId, DateTime? from = null, DateTime? to = null);
    }
}
