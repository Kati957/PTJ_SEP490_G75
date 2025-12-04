using Newtonsoft.Json;

namespace PayOS.Models
    {
    public class Item
        {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public long Price { get; set; }
        }

    public class CreatePaymentRequest
        {
        public string OrderCode { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; }
        public string CancelUrl { get; set; }
        public string ReturnUrl { get; set; }
        public List<Item> Items { get; set; }
        }

    public class CreatePaymentResponse
        {
        public string CheckoutUrl { get; set; }
        }

    public class WebhookData
        {
        public string Signature { get; set; }
        public WebhookDetail Data { get; set; }
        }

    public class WebhookDetail
        {
        public string TransactionId { get; set; }
        public long OrderCode { get; set; }
        public long Amount { get; set; }
        public string Status { get; set; }
        }
    }
