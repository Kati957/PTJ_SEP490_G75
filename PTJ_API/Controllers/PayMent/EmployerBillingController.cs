using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Service.PaymentsService;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using Newtonsoft.Json;

namespace PTJ_API.Controllers.Payment
    {
    [ApiController]
    [Route("api/payment")]
    public class EmployerBillingController : ControllerBase
        {
        private readonly IEmployerPaymentService _payment;
        private readonly JobMatchingDbContext _db;

        public EmployerBillingController(IEmployerPaymentService payment, JobMatchingDbContext db)
            {
            _payment = payment;
            _db = db;
            }

        // ======================================================
        //  Helper: Lấy UserId từ token
        // ======================================================
        private int GetUserId()
            {
            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null)
                throw new UnauthorizedAccessException("Token không hợp lệ.");

            return int.Parse(userClaim.Value);
            }

        // ======================================================
        // 1. Tạo link thanh toán
        // ======================================================
        [Authorize]
        [HttpPost("create-link")]
        public async Task<IActionResult> CreateLink([FromBody] CreatePaymentDto dto)
            {
            int userId = GetUserId();

            string checkoutUrl = await _payment.CreatePaymentLinkAsync(userId, dto.PlanId);

            var trans = await _db.EmployerTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.TransactionId)
                .FirstOrDefaultAsync();

            return Ok(new
                {
                success = true,
                message = "Tạo link thanh toán thành công.",
                checkoutUrl,
                qrCodeUrl = trans?.QrCodeUrl,
                expiredAt = trans?.QrExpiredAt
                });
            }

        // ======================================================
        // 2. Webhook PayOS
        // ======================================================
        [HttpPost("/api/payos/webhook")]
        public async Task<IActionResult> Webhook()
            {
            using var reader = new StreamReader(Request.Body);
            string rawJson = await reader.ReadToEndAsync();

            string signature = Request.Headers["x-payos-signature"];

            await _payment.HandleWebhookAsync(rawJson, signature);

            return Ok(new { received = true });
            }

        // ======================================================
        // 3. Thanh toán thành công
        // ======================================================
        [HttpGet("success")]
        public IActionResult PaymentSuccess()
            {
            return Ok(new { message = "Thanh toán thành công!", status = "SUCCESS" });
            }

        // ======================================================
        // 4. Hủy thanh toán
        // ======================================================
        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel(long orderCode)
            {
            var trans = await _db.EmployerTransactions
                .FirstOrDefaultAsync(x => x.PayOsorderCode == orderCode.ToString());

            if (trans?.Status == "Pending")
                {
                trans.Status = "Cancelled";
                await _db.SaveChangesAsync();
                }

            return Redirect("/payment-failed");
            }

        // ======================================================
        // 5. Lịch sử giao dịch của user
        // ======================================================
        [Authorize]
        [HttpGet("transaction-history")]
        public async Task<IActionResult> GetTransactionHistory()
            {
            int userId = GetUserId();
            var result = await _payment.GetTransactionHistoryAsync(userId);

            return Ok(new { success = true, data = result });
            }

        // ======================================================
        // 6. Lịch sử subscription của user
        // ======================================================
        [Authorize]
        [HttpGet("subscription-history")]
        public async Task<IActionResult> GetSubscriptionHistory()
            {
            int userId = GetUserId();
            var result = await _payment.GetSubscriptionHistoryAsync(userId);

            return Ok(new { success = true, data = result });
            }

        // ======================================================
        // 7. Admin lấy giao dịch theo UserId
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/transactions/{userId}")]
        public async Task<IActionResult> AdminGetTransactionsByUser(int userId)
            {
            var data = await (
                from t in _db.EmployerTransactions
                join u in _db.Users on t.UserId equals u.UserId
                join p in _db.EmployerPlans on t.PlanId equals p.PlanId
                where t.UserId == userId
                orderby t.CreatedAt descending
                select new
                    {
                    t.TransactionId,
                    t.UserId,
                    UserName = u.Username,
                    UserEmail = u.Email,
                    t.PlanId,
                    PlanName = p.PlanName,
                    t.Amount,
                    t.Status,
                    t.PayOsorderCode,
                    t.CreatedAt,
                    t.PaidAt,
                    t.QrCodeUrl,
                    t.QrExpiredAt
                    }
            ).ToListAsync();

            return Ok(new { success = true, data });
            }

        // ======================================================
        // 8. Admin xem subscriptions theo UserId
        // ======================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/subscriptions/{userId}")]
        public async Task<IActionResult> AdminGetSubscriptionByUser(int userId)
            {
            var data = await (
                from sub in _db.EmployerSubscriptions
                join plan in _db.EmployerPlans on sub.PlanId equals plan.PlanId
                join u in _db.Users on sub.UserId equals u.UserId
                where sub.UserId == userId
                orderby sub.StartDate descending
                select new
                    {
                    sub.SubscriptionId,
                    sub.UserId,
                    UserName = u.Username,
                    UserEmail = u.Email,
                    sub.PlanId,
                    plan.PlanName,
                    plan.Price,
                    sub.RemainingPosts,
                    sub.Status,
                    sub.StartDate,
                    sub.EndDate
                    }
            ).ToListAsync();

            return Ok(new { success = true, data });
            }

        // ======================================================
        // 9. API lấy QR hiện tại / tạo QR mới nếu hết hạn
        // ======================================================
        [Authorize]
        [HttpGet("transaction/{id}")]
        public async Task<IActionResult> GetPayment(int id)
            {
            // Lấy userId từ token
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Tìm transaction
            var trans = await _db.EmployerTransactions.FindAsync(id);

            if (trans == null)
                return NotFound(new { message = "Giao dịch không tồn tại" });

            // Kiểm tra có thuộc user hay không
            if (trans.UserId != userId)
                return Forbid("Bạn không có quyền truy cập giao dịch này.");

            // Không cho lấy giao dịch đã hoàn tất
            if (trans.Status != "Pending")
                return BadRequest(new { message = "Giao dịch đã hoàn tất hoặc bị hủy" });

            // ❗ Nếu QR HẾT HẠN → tạo QR mới trên cùng transaction
            if (trans.QrExpiredAt == null || trans.QrExpiredAt <= DateTime.Now)
                {
                string newCheckoutUrl = await _payment.RefreshPaymentLinkAsync(id);

                return Ok(new
                    {
                    expired = true,
                    message = "QR đã hết hạn, hệ thống đã tạo QR mới.",
                    checkoutUrl = newCheckoutUrl,
                    qrCodeUrl = trans.QrCodeUrl,   // QR mới
                    expiredAt = trans.QrExpiredAt
                    });
                }

            // ✔ QR CÒN HẠN → gửi lại QR cũ
            return Ok(new
                {
                expired = false,
                checkoutUrl = (trans.RawWebhookData != null ?
                               JsonConvert.DeserializeObject<dynamic>(trans.RawWebhookData)?.data?.checkoutUrl
                               : null),
                qrCodeUrl = trans.QrCodeUrl,
                expiredAt = trans.QrExpiredAt
                });
            }

        // DTO
        public class CreatePaymentDto
            {
            public int PlanId { get; set; }
            }
        }
    }
