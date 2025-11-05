using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminSolvedReportDto
    {
        public int SolvedReportId { get; set; }
        public int ReportId { get; set; }
        public string ActionTaken { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }
        public string? Reason { get; set; }
        public string? ReportType { get; set; }
        public string? ReportReason { get; set; }
        public DateTime SolvedAt { get; set; }
    }
}
