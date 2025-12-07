using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.AdminDashbroad
{
    public class TopEmployerStatsDto
    {
        public int UserId { get; set; }
        public string EmployerName { get; set; } = string.Empty;
        public int TotalPosts { get; set; }
        public int TotalApplications { get; set; }
    }

}
