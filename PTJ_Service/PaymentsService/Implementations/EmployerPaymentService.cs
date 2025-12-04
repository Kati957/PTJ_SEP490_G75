using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PTJ_Data;
using PTJ_Models.Models;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace PTJ_Service.PaymentsService.Implementations
    {
    public class EmployerPaymentService : IEmployerPaymentService
        {
        private readonly JobMatchingDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _http;

        public EmployerPaymentService(JobMatchingDbContext db, IConfiguration config, IWebHostEnvironment env)
            {
            _db = db;
            _config = config;
            _env = env;
            _http = new HttpClient();
            }

        // ============================
        // 1. CREATE PAYMENT LINK
        // ============================
        public async Task<string> CreatePaymentLinkAsync(int userId, int planId)
            {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new Exception("User không tồn tại");

            var plan = await _db.EmployerPlans.FirstOrDefaultAsync(x => x.PlanId == planId)
                ?? throw new Exception("Gói không tồn tại");

            // Tạo transaction
            var trans = new EmployerTransaction
                {
                UserId = userId,
                PlanId = planId,
                Amount = plan.Price,
                Status = "Pending",
                CreatedAt = DateTime.Now
                };

            _db.EmployerTransactions.Add(trans);
            await _db.SaveChangesAsync();

            // OrderCode >= 6 digits
            long orderCode = 100000 + trans.TransactionId;

            // PAYOS BODY
            var body = new SortedDictionary<string, object?>
            {
                { "orderCode", orderCode },
                { "amount", (int)plan.Price },
                { "description", $"Thanh toán gói {plan.PlanName}" },
                { "returnUrl", _config["PayOS:ReturnUrl"] },
                { "cancelUrl", _config["PayOS:CancelUrl"] }
            };

            // SIGNATURE
            string raw = string.Join("&", body.Select(x => $"{x.Key}={x.Value}"));
            string secret = _config["PayOS:ChecksumKey"];
            string signature = ComputeSignature(raw, secret);
            body.Add("signature", signature);

            // BUILD FULL URL
            string baseUrl = _config["PayOS:BaseUrl"];
            string endpoint = _config["PayOS:CreatePaymentUrl"];
            string fullUrl = baseUrl + endpoint;

            // HEADERS
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

            // SEND REQUEST
            var response = await _http.PostAsJsonAsync(fullUrl, body);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayOS Error => {content}");

            dynamic result = JsonConvert.DeserializeObject(content);

            string checkoutUrl = result.data.checkoutUrl;
            string payOsOrderCode = result.data.orderCode;

            trans.PayOsorderCode = payOsOrderCode;
            await _db.SaveChangesAsync();

            return checkoutUrl;
            }

        private string ComputeSignature(string raw, string secret)
            {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

        // ============================
        // 2. HANDLE WEBHOOK
        // ============================
        public async Task HandleWebhookAsync(string rawJson, string signature)
            {
            Console.WriteLine("📩 RAW WEBHOOK BODY => " + rawJson);

            var payload = JsonConvert.DeserializeObject<JObject>(rawJson);
            if (payload == null)
                {
                Console.WriteLine("❌ Payload null");
                return;
                }

            // --- Lấy object data ---
            JObject data = payload["data"]?.ToObject<JObject>() ?? payload;

            long orderCode = data["orderCode"]?.Value<long>() ?? 0;

            string code = data["code"]?.Value<string>() ?? "";
            string status = data["status"]?.Value<string>() ?? "";

            Console.WriteLine($"📌 orderCode={orderCode}, code={code}, status={status}");

            // --- Verify signature ---
            string checksumKey = _config["PayOS:ChecksumKey"];

            var sorted = new SortedDictionary<string, string>();
            foreach (var prop in data.Properties())
                sorted[prop.Name] = prop.Value?.ToString() ?? "";

            string raw = string.Join("&", sorted.Select(k => $"{k.Key}={k.Value}"));
            string computed = ComputeSignature(raw, checksumKey);

            if (!_env.EnvironmentName.ToLower().Contains("development"))
                {
                if (!string.Equals(signature, computed, StringComparison.OrdinalIgnoreCase))
                    {
                    Console.WriteLine("❌ Sai chữ ký!");
                    return;
                    }
                }

            Console.WriteLine("✅ Webhook hợp lệ!");

            bool success = code == "00" || status.ToUpper() == "PAID";

            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.PayOsorderCode == orderCode.ToString());

            if (trans == null)
                {
                Console.WriteLine($"❌ Transaction not found: {orderCode}");
                return;
                }

            trans.RawWebhookData = rawJson;

            if (success)
                {
                trans.Status = "Paid";
                trans.PaidAt = DateTime.Now;
                await ActivateSubscriptionAsync(trans.UserId, trans.PlanId);
                }

            await _db.SaveChangesAsync();
            }

        // ============================
        // 3. ACTIVATE SUBSCRIPTION
        // ============================
        private async Task ActivateSubscriptionAsync(int userId, int planId)
            {
            var plan = await _db.EmployerPlans.FindAsync(planId);
            if (plan == null) return;

            var old = await _db.EmployerSubscriptions
                .Where(x => x.UserId == userId && x.Status == "Active")
                .ToListAsync();

            foreach (var s in old)
                s.Status = "Expired";

            var now = DateTime.Now;

            var sub = new EmployerSubscription
                {
                UserId = userId,
                PlanId = planId,
                RemainingPosts = plan.MaxPosts,
                StartDate = now,
                EndDate = plan.DurationDays == null ? null : now.AddDays(plan.DurationDays.Value),
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
                };

            _db.EmployerSubscriptions.Add(sub);
            await _db.SaveChangesAsync();
            }
        }
    }
