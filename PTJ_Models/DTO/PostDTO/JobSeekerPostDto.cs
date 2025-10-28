using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Models.DTO.PostDTO
{
    public class JobSeekerPostDto
    {
        public int UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string? PreferredLocation { get; set; }
        public int? CategoryID { get; set; }
        public int? PhoneContact { get; set; }
    }

    public class JobSeekerPostResultDto
    {
        // ✅ Bài đăng mà AI vừa xử lý
        public JobSeekerPostDtoOut Post { get; set; } = new();

        // ✅ Danh sách gợi ý việc làm từ AI
        public List<AIResultDto> SuggestedJobs { get; set; } = new();
    }
}
