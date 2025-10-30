using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminResolveReportDto
    {
       
        public int? AffectedUserId { get; set; }

        public int? AffectedPostId { get; set; }

        public string? AffectedPostType { get; set; }

        public string ActionTaken { get; set; } = null!;

        public string? Reason { get; set; }
    }
}
