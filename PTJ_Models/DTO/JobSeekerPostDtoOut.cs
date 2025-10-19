using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO
{
    public class JobSeekerPostDtoOut
    {
        public int JobSeekerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string? PreferredLocation { get; set; }
        public string? PhoneContact { get; set; }
        public string? CategoryName { get; set; }
        public string? SeekerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }
}
