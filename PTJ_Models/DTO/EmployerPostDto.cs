using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO
{
    public class EmployerPostDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal? Salary { get; set; }

        public string? Requirements { get; set; }

        public string? WorkHours { get; set; }

        public string? Location { get; set; }

        public int? CategoryID { get; set; }

        public string? PhoneContact { get; set; }
    }
}
