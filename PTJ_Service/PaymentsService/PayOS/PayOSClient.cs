using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;

namespace PayOS
    {
    public class PayOSClient
        {
        private readonly HttpClient _http;
        private readonly string _clientId;
        private readonly string _apiKey;

        public PayOSClient(string clientId, string apiKey)
            {
            _clientId = clientId;
            _apiKey = apiKey;

            _http = new HttpClient
                {
                BaseAddress = new Uri("https://api-merchant.payos.vn/")
                };
            }

        // Tạo link thanh toán
        public async Task<JObject> CreatePaymentAsync(object request)
            {
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, "v2/payment-requests");
            req.Content = content;

            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);

            var res = await _http.SendAsync(req);
            string body = await res.Content.ReadAsStringAsync();

            return JObject.Parse(body);
            }

        // Verify webhook bằng API PayOS
        public async Task<JObject> VerifyWebhookAsync(string rawWebhookJson)
            {
            var content = new StringContent(rawWebhookJson, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, "v2/webhook/verify");
            req.Content = content;

            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);

            var res = await _http.SendAsync(req);
            string body = await res.Content.ReadAsStringAsync();

            return JObject.Parse(body);
            }
        }
    }
