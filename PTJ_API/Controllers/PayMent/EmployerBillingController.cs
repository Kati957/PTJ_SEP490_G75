using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Service.PaymentsService;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;

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

        // -----------------------
        // 1. Tạo link thanh toán
        // -----------------------
        [Authorize]
        [HttpPost("create-link")]
        public async Task<IActionResult> CreateLink([FromBody] CreatePaymentDto dto)
            {
            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null)
                return Unauthorized(new { success = false, message = "Token không hợp lệ." });

            int userId = int.Parse(userClaim.Value);

            string checkoutUrl = await _payment.CreatePaymentLinkAsync(userId, dto.PlanId);

            // Nếu EmployerTransaction có thêm QrCodeUrl và QrExpiredAt thì lấy lại
            var lastTrans = await _db.EmployerTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.TransactionId)
                .FirstOrDefaultAsync();

            return Ok(new
                {
                success = true,
                message = "Tạo link thanh toán thành công.",
                checkoutUrl,
                qrCodeUrl = lastTrans?.QrCodeUrl,
                expiredAt = lastTrans?.QrExpiredAt
                });

            }

        // -----------------------
        // 2. Webhook từ PayOS
        // -----------------------
        [HttpPost("/api/payos/webhook")]
        public async Task<IActionResult> Webhook()
            {
            // Đọc RAW JSON do PayOS gửi
            using var reader = new StreamReader(Request.Body);
            string rawJson = await reader.ReadToEndAsync();

            // PayOS gửi chữ ký qua header: x-payos-signature
            string signature = Request.Headers["x-payos-signature"];

            await _payment.HandleWebhookAsync(rawJson, signature);

            return Ok(new { received = true });
            }
        // -----------------------
        // 3. Thanh toán thành công
        // -----------------------
        [HttpGet("success")]
        public IActionResult PaymentSuccess()
            {
            return Ok(new
                {
                message = "Thanh toán thành công!",
                status = "SUCCESS"
                });
            }

        // -----------------------
        // 4. Hủy thanh toán
        // -----------------------
        [HttpGet("cancel")]
        public IActionResult PaymentCancel()
            {
            return Ok(new
                {
                message = "Thanh toán đã bị hủy.",
                status = "CANCELLED"
                });
            }
        }

    // DTO FE gửi vào
    public class CreatePaymentDto
        {
        public int PlanId { get; set; }
        }
    }
