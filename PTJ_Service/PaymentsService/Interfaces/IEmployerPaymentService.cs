using System.Threading.Tasks;
using PTJ_Models.DTO.PaymentEmploy;

namespace PTJ_Service.PaymentsService
    {
    public interface IEmployerPaymentService
        {
        Task<string> CreatePaymentLinkAsync(int userId, int planId);

        /// <summary>
        /// Xử lý Webhook từ PayOS (thành công / thất bại / hết hạn / hủy).
        /// </summary>
        Task HandleWebhookAsync(string rawJson, string signature);

        /// <summary>
        /// Đồng bộ lại trạng thái các giao dịch Pending với PayOS.
        /// Trả về số lượng transaction được cập nhật.
        /// </summary>
        Task<List<EmployerPurchaseDto>> GetActiveSubscriptionsAsync();

        Task<List<EmployerTransactionHistoryDto>> GetTransactionHistoryAsync(int userId);
        Task<List<EmployerSubscriptionHistoryDto>> GetSubscriptionHistoryAsync(int userId);


        }
    }
