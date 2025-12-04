using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PostDTO
{
    // Ensure there is only one definition of the JobSeekerPostDtoOut class in this namespace.
    public class JobSeekerPostDtoOut
    {
        public int JobSeekerPostId { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? PreferredWorkHours { get; set; }

        public string? PreferredWorkHourStart { get; set; }
        public string? PreferredWorkHourEnd { get; set; }

        public string? PreferredLocation { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int WardId { get; set; }

        public string? PhoneContact { get; set; }
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? SeekerName { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Status { get; set; }
        public int? CvId { get; set; } // Added property based on the type signature context.
    }
}
