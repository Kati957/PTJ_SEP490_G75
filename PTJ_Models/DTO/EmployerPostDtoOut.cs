using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO
{
    public class EmployerPostDtoOut
    {
        public int EmployerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Salary { get; set; }
        public string? Requirements { get; set; }
        public string? WorkHours { get; set; }
        public string? Location { get; set; }
        public string? PhoneContact { get; set; }
        public string? CategoryName { get; set; }
        public string? EmployerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
