using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PaymentEmploy
    {
    public class PaymentPlanDto
        {
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public int MaxPosts { get; set; }
        public int? DurationDays { get; set; } // null = vĩnh viễn
        }

    }
