using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminResolveReportDto
    {
        public int AffectedUserId { get; set; } // người bị xử lý (chủ post)
        public string ActionTaken { get; set; } = "BanUser"; // BanUser / Ignore / UnbanUser / DeletePost
        public string? Reason { get; set; } // ghi chú
    }
}
