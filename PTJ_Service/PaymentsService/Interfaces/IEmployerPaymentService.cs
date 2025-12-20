using System.Threading.Tasks;
using PTJ_Models.DTO.PaymentEmploy;

namespace PTJ_Service.PaymentsService
    {
    public interface IEmployerPaymentService
        {
        Task<PaymentLinkResultDto> CreatePaymentLinkAsync(int userId, int planId);
        Task<PaymentLinkResultDto> RefreshPaymentLinkAsync(int transactionId);
        Task HandleWebhookAsync(string rawJson, string signature);

        Task<List<EmployerPurchaseDto>> GetActiveSubscriptionsAsync();
        Task<List<EmployerTransactionHistoryDto>> GetTransactionHistoryAsync(int userId);
        Task<List<EmployerSubscriptionHistoryDto>> GetSubscriptionHistoryAsync(int userId);

        Task VerifyAndFinalizePaymentAsync(long orderCode);

        }

    }
