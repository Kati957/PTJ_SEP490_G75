using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.AdminDashbroad
{
    public class NewsStatsDto
    {
        public DateTime? Date { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; }
        public int Count { get; set; }
    }
}
