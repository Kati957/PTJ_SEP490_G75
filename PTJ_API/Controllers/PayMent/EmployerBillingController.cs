using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PTJ_Service.PaymentsService;

namespace PTJ_API.Controllers.Payment
    {
    [ApiController]
    [Route("api/payment")]
    public class EmployerBillingController : ControllerBase
        {
        private readonly IEmployerPaymentService _payment;

        public EmployerBillingController(IEmployerPaymentService payment)
            {
            _payment = payment;
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

            string url = await _payment.CreatePaymentLinkAsync(userId, dto.PlanId);

            return Ok(new { checkoutUrl = url });
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
