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
        public decimal MonthGrowthPercent { get; set; }
        public int TotalTransactions { get; set; }
        public string? BestSellingPlan { get; set; }
    }

    public class RevenueStatsDto
    {
        public DateTime? Date { get; set; } 
        public int Year { get; set; }
        public int? Month { get; set; }
        public decimal Revenue { get; set; }

        public string Label =>
            Date != null
                ? Date.Value.ToString("dd/MM/yyyy")
                : Month != null
                    ? $"{Month:00}/{Year}"
                    : Year.ToString();
    }

    public class RevenueByPlanDto
    {
        public string PlanName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Transactions { get; set; }
        public int Users { get; set; }
        public decimal SuccessRate { get; set; }
    }

}
