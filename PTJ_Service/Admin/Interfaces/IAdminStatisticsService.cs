using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.Admin;

namespace PTJ_Service.Admin.Interfaces
{
        public interface IAdminStatisticsService
        {
            Task<AdminStatisticsDto> GetAdminStatisticsAsync();
        }
}
