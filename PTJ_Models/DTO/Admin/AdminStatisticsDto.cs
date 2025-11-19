using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalEmployers { get; set; }
        public int TotalJobSeekers { get; set; }
        public int TotalEmployerPosts { get; set; }
        public int TotalJobSeekerPosts { get; set; }
        public int TotalReports { get; set; }
        public int TotalNews { get; set; }
    }

}
