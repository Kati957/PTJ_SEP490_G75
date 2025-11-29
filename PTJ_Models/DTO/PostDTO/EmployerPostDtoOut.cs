using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PostDTO
{
    public class EmployerPostDtoOut
    {
        public int EmployerPostId { get; set; }
        public int EmployerId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal? Salary { get; set; }
        public string? SalaryText { get; set; }

        public string? Requirements { get; set; }
        public string? WorkHours { get; set; }
        public string? WorkHourStart { get; set; }
        public string? WorkHourEnd { get; set; }

        public string? ExpiredAtText { get; set; }  // format dd/MM/yyyy để client dùng thẳng

        public string? Location { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int WardId { get; set; }

        public string? PhoneContact { get; set; }
        public string? CategoryName { get; set; }
        public string EmployerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<string>? ImageUrls { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
    }

}
