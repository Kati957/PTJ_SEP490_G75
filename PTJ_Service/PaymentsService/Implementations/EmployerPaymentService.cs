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

            long orderCode = 100000 + trans.TransactionId;

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
            Console.WriteLine("📩 RAW WEBHOOK => " + rawJson);

            var payload = JsonConvert.DeserializeObject<JObject>(rawJson);
            if (payload == null)
                {
                Console.WriteLine("❌ Payload null");
                return;
                }

            JObject data = payload["data"]?.ToObject<JObject>() ?? payload;

            long orderCode = data["orderCode"]?.Value<long>() ?? 0L;
            string code = data["code"]?.Value<string>() ?? "";
            string status = data["status"]?.Value<string>() ?? "";

            Console.WriteLine($"📌 orderCode={orderCode}, code={code}, status={status}");

            // 3. Verify signature
            string checksumKey = _config["PayOS:ChecksumKey"];

            var sorted = new SortedDictionary<string, string>();
            foreach (var prop in data.Properties())
                sorted[prop.Name] = prop.Value?.ToString() ?? "";

            string raw = string.Join("&", sorted.Select(x => $"{x.Key}={x.Value}"));
            string computed = ComputeSignature(raw, checksumKey);

            if (!_env.EnvironmentName.ToLower().Contains("development"))
                {
                if (!string.Equals(signature, computed, StringComparison.OrdinalIgnoreCase))
                    {
                    Console.WriteLine("❌ Signature mismatch!");
                    return;
                    }
                }

            Console.WriteLine("✅ Signature valid!");

            // 4. Lấy transaction local
            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.PayOsorderCode == orderCode.ToString());

            if (trans == null)
                {
                Console.WriteLine("❌ Transaction not found");
                return;
                }

            // Tránh xử lý lại
            if (trans.Status == "Paid")
                {
                Console.WriteLine("⚠ Webhook đã xử lý trước đó, bỏ qua.");
                return;
                }

            trans.RawWebhookData = rawJson;

            // 5. Xác định kết quả thanh toán
            string st = status.ToUpper();
            bool success = (code == "00" || st == "PAID");

            if (success)
                {
                trans.Status = "Paid";
                trans.PaidAt = DateTime.Now;

                await ActivateSubscriptionAsync(trans.UserId, trans.PlanId);
                }
            else if (st == "EXPIRED")
                trans.Status = "Expired";
            else if (st == "CANCELLED")
                trans.Status = "Cancelled";
            else
                trans.Status = "Failed";

            await _db.SaveChangesAsync();
            }

        // ============================
        // 3. SYNC PENDING WITH PAYOS
        // ============================
        /// <summary>
        /// Đồng bộ lại trạng thái các transaction Pending với PayOS.
        /// Gợi ý: gọi từ BackgroundService (VD: mỗi 1–5 phút).
        /// </summary>
        public async Task<int> SyncPendingTransactionsAsync(int maxAgeMinutes = 5)
            {
            var now = DateTime.Now;
            var cutoff = now.AddMinutes(-maxAgeMinutes);

            var pendingList = await _db.EmployerTransactions
                .Where(x =>
                    x.Status == "Pending" &&
                    x.PayOsorderCode != null &&
                    x.CreatedAt <= cutoff)
                .ToListAsync();

            if (!pendingList.Any())
                return 0;

            int updated = 0;

            foreach (var trans in pendingList)
                {
                if (!long.TryParse(trans.PayOsorderCode, out var orderCode))
                    continue;

                string status = await QueryPayOsStatusAsync(orderCode);
                string statusUpper = status.ToUpper();

                // Không có gì thay đổi
                if (statusUpper == "PENDING" || string.IsNullOrWhiteSpace(statusUpper))
                    continue;

                // Mapping
                if (statusUpper == "PAID")
                    {
                    if (trans.Status != "Paid")
                        {
                        trans.Status = "Paid";
                        trans.PaidAt = now;
                        await ActivateSubscriptionAsync(trans.UserId, trans.PlanId);
                        updated++;
                        }
                    }
                else if (statusUpper == "CANCELLED")
                    {
                    if (trans.Status != "Cancelled")
                        {
                        trans.Status = "Cancelled";
                        updated++;
                        }
                    }
                else if (statusUpper == "EXPIRED")
                    {
                    if (trans.Status != "Expired")
                        {
                        trans.Status = "Expired";
                        updated++;
                        }
                    }
                else if (statusUpper == "FAILED")
                    {
                    if (trans.Status != "Failed")
                        {
                        trans.Status = "Failed";
                        updated++;
                        }
                    }
                }

            if (updated > 0)
                await _db.SaveChangesAsync();

            return updated;
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
        // 5. QUERY PAYOS STATUS
        // ============================
        /// <summary>
        /// Gọi API PayOS để lấy trạng thái orderCode hiện tại.
        /// </summary>
        private async Task<string> QueryPayOsStatusAsync(long orderCode)
            {
            string baseUrl = _config["PayOS:BaseUrl"];

            // TODO: chỉnh lại endpoint cho đúng với docs PayOS của bạn.
            // Ví dụ: "/v2/payment-requests/{orderCode}"
            string endpoint = $"/v2/payment-requests/{orderCode}";
            string url = baseUrl + endpoint;

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

            var resp = await _http.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                {
                Console.WriteLine($"❌ QueryPayOsStatusAsync error: {json}");
                return string.Empty;
                }

            dynamic result = JsonConvert.DeserializeObject(json);
            string status = result.data.status;
            Console.WriteLine($"🔄 PayOS status for {orderCode} => {status}");
            return status;
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
        }
    }
