using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PTJ_Data.Repositories.Interfaces.Users;
using PTJ_Models.Models;

namespace PTJ_Service.UserActivityService
{
    public class UserActivityService : IUserActivityService
    {
        private readonly IUserActivityRepository _repo;
        private readonly IHttpContextAccessor _http;

        public UserActivityService(IUserActivityRepository repo, IHttpContextAccessor http)
        {
            _repo = repo;
            _http = http;
        }

        public async Task LogAsync(int userId, string activityType, string details)
        {
            var httpContext = _http.HttpContext;

            var log = new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                Details = details,
                Ipaddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                DeviceInfo = httpContext?.Request?.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.Now
            };

            await _repo.AddAsync(log);
        }

        public async Task<IEnumerable<UserActivityLog>> GetHistoryAsync(int userId, DateTime? from = null, DateTime? to = null)
        {
            return await _repo.GetByUserAsync(userId, from, to);
        }
    }
}
