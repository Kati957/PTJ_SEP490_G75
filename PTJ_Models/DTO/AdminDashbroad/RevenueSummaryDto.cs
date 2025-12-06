using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.AdminDashbroad
{
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public decimal GrowthPercent { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenueByPlanDto
    {
        public string PlanName { get; set; }
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }
}
