using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

namespace PTJ_Service.Admin.Implementations
{
    public class AdminStatisticsService : IAdminStatisticsService
    {
        private readonly IAdminStatisticsRepository _repo;

        public AdminStatisticsService(IAdminStatisticsRepository repo)
        {
            _repo = repo;
        }

        public Task<AdminStatisticsDto> GetAdminStatisticsAsync()
        {
            return _repo.GetAdminStatisticsAsync();
        }
    }
}
