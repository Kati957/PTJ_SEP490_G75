using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.FilterDTO
    {
    public class SalarySearchDto
        {
        public bool Negotiable { get; set; } = false;  
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
        public bool IncludeNegotiable { get; set; } = true; 
        }
    }
