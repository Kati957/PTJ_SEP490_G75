using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.AdminDashbroad
{
    public class PostStatsByDayDto
    {
        public DateTime Date { get; set; }
        public int EmployerPosts { get; set; }
        public int JobSeekerPosts { get; set; }
        public int Total => EmployerPosts + JobSeekerPosts;
    }
    public class PostStatsByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int EmployerPosts { get; set; }
        public int JobSeekerPosts { get; set; }
        public int Total => EmployerPosts + JobSeekerPosts;

        public string Label => Month.ToString("00") + "/" + Year;
    }
    public class PostStatsByYearDto
    {
        public int Year { get; set; }
        public int EmployerPosts { get; set; }
        public int JobSeekerPosts { get; set; }
        public int Total => EmployerPosts + JobSeekerPosts;
    }


}
