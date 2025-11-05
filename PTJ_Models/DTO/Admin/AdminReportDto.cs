using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public string? TargetUserEmail { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
