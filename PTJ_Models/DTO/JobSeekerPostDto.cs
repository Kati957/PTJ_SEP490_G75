using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Models.Models;

namespace PTJ_Models.DTO
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
        public string? PhoneContact { get; set; }
    }

    public class JobSeekerPostResultDto
    {
        public PTJ_Models.Models.JobSeekerPost Post { get; set; } = new();
        public List<AIResultDto> SuggestedJobs { get; set; } = new();
    }
}
