using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.AdminDashbroad
{
    public class SubscriptionStatsDto
    {
        public int Free { get; set; }
        public int Medium { get; set; }
        public int Premium { get; set; }
        public int Active { get; set; }
        public int Expired { get; set; }
    }
}
