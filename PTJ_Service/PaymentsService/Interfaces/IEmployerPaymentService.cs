using System.Threading.Tasks;
public interface IEmployerPaymentService
    {
    Task<string> CreatePaymentLinkAsync(int userId, int planId);
    Task HandleWebhookAsync(string rawBody, string signature);
    }
