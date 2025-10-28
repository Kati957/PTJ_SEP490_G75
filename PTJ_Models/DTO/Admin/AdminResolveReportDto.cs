using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminResolveReportDto
    {
        /// <summary>ID người bị ảnh hưởng (nếu report user)</summary>
        public int? AffectedUserId { get; set; }

        /// <summary>ID bài đăng bị ảnh hưởng (nếu report post)</summary>
        public int? AffectedPostId { get; set; }

        /// <summary>Loại bài đăng: "EmployerPost" hoặc "JobSeekerPost"</summary>
        public string? AffectedPostType { get; set; }

        /// <summary>Hành động xử lý: BanUser, UnbanUser, DeletePost, Warn, Ignore</summary>
        public string ActionTaken { get; set; } = null!;

        /// <summary>Lý do xử lý (nếu có)</summary>
        public string? Reason { get; set; }
    }
}
