using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PTJ_Data;
using PTJ_Models.DTO.PaymentEmploy;
using PTJ_Models.Models;
using PTJ_Service.Helpers.Implementations;
using PTJ_Service.Helpers.Interfaces;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using PTJ_Models.DTO.PaymentEmploy;
using Microsoft.AspNetCore.SignalR;
using PTJ_Service.Hubs;

namespace PTJ_Service.PaymentsService.Implementations
{
    public class EmployerPaymentService : IEmployerPaymentService
    {
        private readonly JobMatchingOpenAiDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _http;
        private readonly IHubContext<PaymentHub> _hub;
        private readonly IEmailTemplateService _emailTemplate;
        private readonly SmtpEmailSender _smtpEmailSender;
        public EmployerPaymentService(
            JobMatchingOpenAiDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            IHubContext<PaymentHub> hub,
            IEmailTemplateService emailTemplate,
            SmtpEmailSender emailSender)
        {
            _emailTemplate = emailTemplate;
            _smtpEmailSender = emailSender;
            _db = db;
            _config = config;
            _env = env;
            _hub = hub;
            _http = new HttpClient();
        }

        // ============================
        // 1. CREATE PAYMENT LINK
        // ============================
        public async Task<PaymentLinkResultDto> CreatePaymentLinkAsync(int userId, int planId)
        {
            // 0. Validate user
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new Exception("User không tồn tại");

            if (!user.IsActive)
                throw new Exception("Tài khoản của bạn đang bị khóa. Không thể thanh toán.");

            // 1. Tìm giao dịch Pending CÙNG GÓI của user
            var oldPending = await _db.EmployerTransactions
                .Where(x => x.UserId == userId && x.Status == "Pending" && x.PlanId == planId)
                .OrderByDescending(x => x.TransactionId)
                .FirstOrDefaultAsync();

            // ⚠ Có pending cùng gói
            if (oldPending != null)
            {
                // QR còn hạn → dùng lại luôn
                if (oldPending.QrExpiredAt.HasValue && oldPending.QrExpiredAt > DateTime.Now)
                {
                    var existingCheckoutUrl = ExtractCheckoutUrl(oldPending.RawWebhookData);

                    return new PaymentLinkResultDto
                    {
                        TransactionId = oldPending.TransactionId,
                        CheckoutUrl = existingCheckoutUrl,
                        OrderCode = oldPending.PayOsorderCode,
                        QrCodeRaw = oldPending.QrCodeUrl ?? string.Empty,
                        ExpiredAt = oldPending.QrExpiredAt
                    };
                }

                // QR hết hạn → refresh trên transaction cũ
                return await RefreshPaymentLinkAsync(oldPending.TransactionId);
            }

            // 2. Không có pending transaction → tạo mới

            // 2.1 Kiểm tra gói
            var plan = await _db.EmployerPlans.FirstOrDefaultAsync(x => x.PlanId == planId)
                ?? throw new Exception("Gói không tồn tại");

            if (plan.Price <= 0)
                throw new Exception("Giá gói không hợp lệ.");

            int amount = (int)plan.Price;

            // 2.2 Kiểm tra gói đang active
            var activeSub = await _db.EmployerSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "Active");

            if (activeSub != null)
                {
                var oldPlan = await _db.EmployerPlans.FindAsync(activeSub.PlanId);
                var newPlan = plan;

                if (activeSub.PlanId == planId)
                    throw new Exception("Bạn đang sử dụng gói này. Không thể mua lại.");

                if (newPlan.Price < oldPlan.Price)
                    throw new Exception("Không thể hạ cấp gói.");

                // --- NÂNG CẤP: CHỈ GIẢM GIÁ KHI CÒN BÀI ---
                if (activeSub.RemainingPosts > 0)
                    {
                    decimal valuePerPost = oldPlan.Price / oldPlan.MaxPosts;
                    decimal remainingValue = activeSub.RemainingPosts * valuePerPost;

                    int upgradeAmount = (int)(newPlan.Price - remainingValue);
                    if (upgradeAmount < 0)
                        upgradeAmount = 0;

                    amount = upgradeAmount;
                    }
                else
                    {
                    // Hết bài => Mua gói mới với giá gốc
                    amount = (int)newPlan.Price;
                    }
                }

            // 3. Tạo transaction local
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

            // 4. Tạo orderCode PayOS
            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

            // 5. Build PayOS body + signature
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

            // 6. Gọi PayOS
            string fullUrl = _config["PayOS:BaseUrl"] + _config["PayOS:CreatePaymentUrl"];

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

            var response = await _http.PostAsJsonAsync(fullUrl, body);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayOS Error => {content}");

            dynamic result = JsonConvert.DeserializeObject(content);

            if (result == null || result.data == null)
                throw new Exception($"PayOS trả về dữ liệu không hợp lệ: {content}");

            // 7. Lấy link thanh toán
            string checkoutUrl = result.data.checkoutUrl;
            string payOsOrderCode = result.data.orderCode;
            string qrRaw = result.data.qrCode;   // PayOS trả QR RAW

            // 8. Lưu lại vào transaction (GIỮ RAW)
            trans.PayOsorderCode = payOsOrderCode;
            trans.RawWebhookData = content;
            trans.QrCodeUrl = qrRaw;            // chứa QR RAW
            trans.QrExpiredAt = DateTime.Now.AddMinutes(2);

            await _db.SaveChangesAsync();

            // 9. Trả DTO cho controller
            return new PaymentLinkResultDto
            {
                TransactionId = trans.TransactionId,
                CheckoutUrl = checkoutUrl,
                QrCodeRaw = qrRaw,
                ExpiredAt = trans.QrExpiredAt,
                OrderCode = payOsOrderCode
            };
        }

        // Helper: Extract checkoutUrl từ RawWebhookData (khi còn pending)
        private string? ExtractCheckoutUrl(string? rawWebhookData)
        {
            if (string.IsNullOrWhiteSpace(rawWebhookData)) return null;

            try
            {
                dynamic obj = JsonConvert.DeserializeObject(rawWebhookData);
                return obj?.data?.checkoutUrl;
            }
            catch
            {
                return null;
            }
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

            // --- Lấy signature từ BODY webhook ---
            string receivedSignature = payload["signature"]?.Value<string>() ?? "";
            string checksumKey = _config["PayOS:ChecksumKey"];

            // Build data dùng để ký lại: chính là object "data" trong webhook
            var sorted = new SortedDictionary<string, string>();
            foreach (var prop in data.Properties())
                {
                var value = prop.Value?.ToString() ?? "";

                // PayOS coi "null", "undefined" là chuỗi rỗng
                if (value == "null" || value == "undefined")
                    value = "";

                sorted[prop.Name] = value;
                }

            // Tạo chuỗi key1=value1&key2=value2...
            string raw = string.Join("&", sorted.Select(k => $"{k.Key}={k.Value}"));

            // Tính lại chữ ký
            string computed = ComputeSignature(raw, checksumKey);

            if (!_env.EnvironmentName.ToLower().Contains("development"))
                {
                if (!string.Equals(receivedSignature, computed, StringComparison.OrdinalIgnoreCase))
                    {
                    Console.WriteLine("❌ Sai chữ ký!");
                    Console.WriteLine($"Expected: {computed}");
                    Console.WriteLine($"Received: {receivedSignature}");
                    return;
                    }
                }

            Console.WriteLine("✅ Webhook hợp lệ!");


            bool success = code == "00" || status.ToUpper() == "PAID";

            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.PayOsorderCode == orderCode.ToString());

            if (trans == null)
                throw new Exception($"Transaction not found: {orderCode}");

            if (trans.Status == "Paid")
                return; // webhook retry → bỏ qua

            trans.RawWebhookData = rawJson;

            if (success)
            {
                trans.Status = "Paid";
                trans.PaidAt = DateTime.Now;

                // Kích hoạt subscription
                await ActivateSubscriptionAsync(trans.UserId, trans.PlanId);

                

                // Sau đó gửi email (dùng subscription vừa kích hoạt)
                await SendPaymentSuccessEmailToEmployerAsync(trans);
                await _db.SaveChangesAsync();
               // Bắn realtime cho FE
                await _hub.Clients.User(trans.UserId.ToString())
                    .SendAsync("PaymentStatusChanged", new
                        {
                        orderCode = trans.PayOsorderCode,
                        status = "Paid",
                        planId = trans.PlanId,
                        paidAt = trans.PaidAt
                        });
                }
            }

        // ============================
        // 4. ACTIVATE SUBSCRIPTION
        // ============================
        private async Task ActivateSubscriptionAsync(int userId, int planId)
        {
            bool alreadyActive = await _db.EmployerSubscriptions.AnyAsync(x =>
        x.UserId == userId &&
        x.PlanId == planId &&
        x.Status == "Active");

            if (alreadyActive)
                return;

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
            //await _db.SaveChangesAsync();
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

        public async Task<PaymentLinkResultDto> RefreshPaymentLinkAsync(int transactionId)
        {
            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId)
                ?? throw new Exception("Transaction không tồn tại");

            if (trans.Status != "Pending")
                throw new Exception("Chỉ làm mới QR cho giao dịch Pending");

            // QR còn hạn → không cho refresh
            if (trans.QrExpiredAt != null && trans.QrExpiredAt > DateTime.Now)
                throw new Exception("QR code vẫn còn hạn, không cần refresh.");

            int amount = (int)(trans.Amount ?? 0);

            // Tạo order code mới
            long newOrderCode = long.Parse(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

            var body = new SortedDictionary<string, object?>
            {
                { "orderCode", newOrderCode },
                { "amount", amount },
                { "description", $"Thanh toán gói {trans.PlanId}" },
                { "returnUrl", _config["PayOS:ReturnUrl"] },
                { "cancelUrl", _config["PayOS:CancelUrl"] }
            };

            string raw = string.Join("&", body.Select(x => $"{x.Key}={x.Value}"));
            string signature = ComputeSignature(raw, _config["PayOS:ChecksumKey"]);
            body.Add("signature", signature);

            string fullUrl = _config["PayOS:BaseUrl"] + _config["PayOS:CreatePaymentUrl"];

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);

            var response = await _http.PostAsJsonAsync(fullUrl, body);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PayOS Error => {content}");

            dynamic result = JsonConvert.DeserializeObject(content);
            if (result?.data == null)
                throw new Exception("PayOS trả về dữ liệu không hợp lệ");

            // Lấy link mới
            string checkoutUrl = result.data.checkoutUrl;
            string qrRaw = result.data.qrCode;
            string payOsOrderCode = result.data.orderCode;

            // Cập nhật transaction cũ
            trans.PayOsorderCode = payOsOrderCode;
            trans.QrCodeUrl = qrRaw;
            trans.QrExpiredAt = DateTime.Now.AddMinutes(2);
            trans.RawWebhookData = content;

            await _db.SaveChangesAsync();

            return new PaymentLinkResultDto
            {
                TransactionId = trans.TransactionId,
                CheckoutUrl = checkoutUrl,
                QrCodeRaw = qrRaw,
                ExpiredAt = trans.QrExpiredAt,
                OrderCode = payOsOrderCode
            };
        }

        // Gửi email thanh toán thành công cho Employer
        private async Task SendPaymentSuccessEmailToEmployerAsync(EmployerTransaction trans)
            {
            if (trans.EmailSent)
                return;

            var user = await _db.Users.FindAsync(trans.UserId);
            var plan = await _db.EmployerPlans.FindAsync(trans.PlanId);

            if (user == null || plan == null)
                return;

            var sub = await _db.EmployerSubscriptions
                .Where(x => x.UserId == trans.UserId
                         && x.PlanId == trans.PlanId
                         && x.Status == "Active")
                .OrderByDescending(x => x.SubscriptionId)
                .FirstOrDefaultAsync();

            if (sub == null)
                return;

            string html = _emailTemplate.CreateEmployerPaymentSuccessTemplate(
                employerName: user.Username,
                planName: plan.PlanName,
                amount: trans.Amount ?? 0,
                remainingPosts: sub.RemainingPosts,
                startDate: sub.StartDate,
                endDate: sub.EndDate
            );

            await _smtpEmailSender.SendEmailAsync(
                user.Email,
                "Thanh toán thành công",
                html
            );

            trans.EmailSent = true;
            await _db.SaveChangesAsync();
            }


        public async Task VerifyAndFinalizePaymentAsync(long orderCode)
            {
            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.PayOsorderCode == orderCode.ToString());

            if (trans == null)
                return;

            if (trans.Status == "Paid" && trans.EmailSent)
                return;

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", _config["PayOS:ClientId"]);
            _http.DefaultRequestHeaders.Add("x-api-key", _config["PayOS:ApiKey"]);


            // CALL PAYOS VERIFY API
            var response = await _http.GetAsync(
                $"{_config["PayOS:BaseUrl"]}/v2/payment-requests/{orderCode}");

            var content = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(content);

            if (result?.data?.status == "PAID")
                {
                trans.Status = "Paid";
                trans.PaidAt = DateTime.Now;

                await ActivateSubscriptionAsync(trans.UserId, trans.PlanId);
                await SendPaymentSuccessEmailToEmployerAsync(trans);

                await _db.SaveChangesAsync();
                }
            }

        }
    }