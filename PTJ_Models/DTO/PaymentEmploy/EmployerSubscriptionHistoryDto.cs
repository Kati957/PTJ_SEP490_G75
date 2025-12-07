using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PaymentEmploy
    {
    public class EmployerSubscriptionHistoryDto
        {
        public int SubscriptionId { get; set; }
        public string PlanName { get; set; } = "";
        public decimal? Price { get; set; }  // FIX HERE

        public int RemainingPosts { get; set; }
        public string Status { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // FIX HERE
        }

    }
