using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.HomePageDTO
{
    public class HomeStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveEmployerPosts { get; set; }
        public int ActiveJobSeekerPosts { get; set; }
        public int TotalCategories { get; set; }
    }
}
