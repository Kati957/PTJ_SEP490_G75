namespace PTJ_Models.DTO.PaymentEmploy
    {
    public class PaymentLinkResultDto
        {
        public int TransactionId { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCodeRaw { get; set; } = string.Empty;
        public DateTime? ExpiredAt { get; set; }
        }
    }
