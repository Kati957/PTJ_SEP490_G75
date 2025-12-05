using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PaymentEmploy
    {
    public class EmployerTransactionHistoryDto
        {
        public int TransactionId { get; set; }
        public string Status { get; set; } = "";

        public decimal? Amount { get; set; }      // ✔ ĐÃ SỬA – nullable

        public string? PayOSOrderCode { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public int PlanId { get; set; }
        public DateTime? QrExpiredAt { get; set; }
        public string? QrCodeUrl { get; set; }
        }

    }
