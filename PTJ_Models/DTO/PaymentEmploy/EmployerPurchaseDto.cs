using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PaymentEmploy
    {
    public class EmployerPurchaseDto
        {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }

        public int PlanId { get; set; }
        public string? PlanName { get; set; }
        public decimal? Price { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int RemainingPosts { get; set; }
        public string Status { get; set; } = string.Empty;
        }

    }
