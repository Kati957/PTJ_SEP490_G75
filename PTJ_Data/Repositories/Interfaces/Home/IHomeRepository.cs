using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.DTO.HomePageDTO;

namespace PTJ_Data.Repositories.Interfaces.Home
{
    public interface IHomeRepository
    {
        Task<HomeStatisticsDto> GetHomeStatisticsAsync();
    }
}
