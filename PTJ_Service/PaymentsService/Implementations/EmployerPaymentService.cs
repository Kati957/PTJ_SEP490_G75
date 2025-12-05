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
using PTJ_Models.DTO.PaymentEmploy;

namespace PTJ_Service.PaymentsService.Implementations
    {
    public class EmployerPaymentService : IEmployerPaymentService
        {
        private readonly JobMatchingDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _http;

        public EmployerPaymentService(
            JobMatchingDbContext db,
            IConfiguration config,
            IWebHostEnvironment env)
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
            // 0. Validate user
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new Exception("User không tồn tại");

            if (!user.IsActive)
                throw new Exception("Tài khoản của bạn đang bị khóa. Không thể thanh toán.");

            // 1. Kiểm tra PENDING transaction (chỉ chặn những cái mới < 5 phút)
            var cutoff = DateTime.Now.AddMinutes(-5);
            var pending = await _db.EmployerTransactions
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.Status == "Pending" &&
                    x.CreatedAt >= cutoff);

            if (pending)
                throw new Exception("Bạn đang có giao dịch thanh toán chưa hoàn tất. Vui lòng hoàn tất hoặc đợi hệ thống đồng bộ.");

            // 2. Kiểm tra gói
            var plan = await _db.EmployerPlans.FirstOrDefaultAsync(x => x.PlanId == planId)
                ?? throw new Exception("Gói không tồn tại");

            if (plan.Price <= 0)
                throw new Exception("Giá gói không hợp lệ.");

            int amount = (int)plan.Price;

            // 3. Kiểm tra Employer đang có gói active
            var activeSub = await _db.EmployerSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "Active");

            if (activeSub != null && activeSub.PlanId == planId)
                throw new Exception("Bạn đang sử dụng gói này. Không thể mua lại.");

            // (Option) Không cho phép mua gói khác khi gói cũ còn hạn
            if (activeSub != null && activeSub.PlanId != planId && activeSub.EndDate > DateTime.Now)
                throw new Exception("Bạn đang có gói khác còn hạn. Không thể mua gói mới.");

            // 4. Tạo transaction local
            var trans = new EmployerTransaction
                {
                UserId = userId,
                PlanId = planId,
                Amount = amount,
                Status = "Pending",
                CreatedAt = DateTime.Now
                };

            _db.EmployerTransactions.Add(trans);
            await _db.SaveChangesAsync();

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());



            // 5. Build PayOS body + checksum
            var body = new SortedDictionary<string, object?>
            {
                { "orderCode", orderCode },
                { "amount", amount },
                { "description", $"Thanh toán gói {plan.PlanName}" },
                { "returnUrl", _config["PayOS:ReturnUrl"] },
                { "cancelUrl", _config["PayOS:CancelUrl"] }
            };

            string raw = string.Join("&", body.Select(x => $"{x.Key}={x.Value}"));
            string secret = _config["PayOS:ChecksumKey"];
            string signature = ComputeSignature(raw, secret);
            body.Add("signature", signature);

            // 6. Gọi API PayOS
            string baseUrl = _config["PayOS:BaseUrl"];
            string endpoint = _config["PayOS:CreatePaymentUrl"]; // VD: "/v2/payment-requests"
            string fullUrl = baseUrl + endpoint;

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

            var response = await _http.PostAsJsonAsync(fullUrl, body);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayOS Error => {content}");

            dynamic result = JsonConvert.DeserializeObject(content);

            if (result == null || result.data == null)
                {
                throw new Exception($"PayOS trả về dữ liệu không hợp lệ: {content}");
                }

            // 7. Lấy dữ liệu từ PayOS
            string checkoutUrl = result.data.checkoutUrl;
            string payOsOrderCode = result.data.orderCode;

            string? qrCodeUrl = result.data.qrCodeUrl;
            long? expiredAtUnix = result.data.expiredAt;

            DateTime? expiredAt = expiredAtUnix != null
                ? DateTimeOffset.FromUnixTimeSeconds((long)expiredAtUnix).LocalDateTime
                : null;

            // 8. Cập nhật lại transaction
            trans.PayOsorderCode = payOsOrderCode;
            trans.RawWebhookData = content;
            trans.QrCodeUrl = qrCodeUrl;
            trans.QrExpiredAt = expiredAt;

            await _db.SaveChangesAsync();

            return checkoutUrl;
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
        // 4. ACTIVATE SUBSCRIPTION
        // ============================
        private async Task ActivateSubscriptionAsync(int userId, int planId)
            {
            var plan = await _db.EmployerPlans.FindAsync(planId);
            if (plan == null) return;

            var oldSubs = await _db.EmployerSubscriptions
                .Where(x => x.UserId == userId && x.Status == "Active")
                .ToListAsync();

            foreach (var s in oldSubs)
                s.Status = "Expired";

            var now = DateTime.Now;

            var newSub = new EmployerSubscription
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

            _db.EmployerSubscriptions.Add(newSub);
            await _db.SaveChangesAsync();
            }

        // ============================
        // 6. HMAC SIGNATURE
        // ============================
        private string ComputeSignature(string raw, string secret)
            {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

        public async Task<List<EmployerPurchaseDto>> GetActiveSubscriptionsAsync()
            {
            var result = await (
                from sub in _db.EmployerSubscriptions
                join user in _db.Users on sub.UserId equals user.UserId
                join plan in _db.EmployerPlans on sub.PlanId equals plan.PlanId
                where sub.Status == "Active"
                select new EmployerPurchaseDto
                    {
                    UserId = sub.UserId,
                    FullName = user.Username,
                    Email = user.Email,
                    PlanId = sub.PlanId,
                    PlanName = plan.PlanName,
                    Price = plan.Price,

                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate,
                    RemainingPosts = sub.RemainingPosts,
                    Status = sub.Status
                    }
            ).ToListAsync();

            return result;
            }

        public async Task<List<EmployerTransactionHistoryDto>> GetTransactionHistoryAsync(int userId)
            {
            var items = await _db.EmployerTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new EmployerTransactionHistoryDto
                    {
                    TransactionId = x.TransactionId,
                    Status = x.Status,
                    Amount = x.Amount,
                    PayOSOrderCode = x.PayOsorderCode,
                    CreatedAt = x.CreatedAt,
                    PaidAt = x.PaidAt,

                    PlanId = x.PlanId,                
                    QrExpiredAt = x.QrExpiredAt,    
                    QrCodeUrl = x.QrCodeUrl        
                    })
                .ToListAsync();

            return items;
            }


        public async Task<List<EmployerSubscriptionHistoryDto>> GetSubscriptionHistoryAsync(int userId)
            {
            var items = await (
                from sub in _db.EmployerSubscriptions
                join plan in _db.EmployerPlans on sub.PlanId equals plan.PlanId
                where sub.UserId == userId
                orderby sub.StartDate descending
                select new EmployerSubscriptionHistoryDto
                    {
                    SubscriptionId = sub.SubscriptionId,
                    PlanName = plan.PlanName,
                    Price = plan.Price,
                    RemainingPosts = sub.RemainingPosts,
                    Status = sub.Status,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate
                    }
            ).ToListAsync();

            return items;
            }

        }
    }
