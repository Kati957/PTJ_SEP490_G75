using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO.PaymentEmploy;
using PTJ_Models.Models;

namespace PTJ_API.Controllers.AdminController
    {
    [Route("api/[controller]")]
    [ApiController]
    public class AdminPlansController : ControllerBase
        {
        private readonly JobMatchingOpenAiDbContext _db;
        public AdminPlansController(JobMatchingOpenAiDbContext db)
            {
            _db = db;
            }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/plans")]
        public async Task<IActionResult> GetAllPlans()
            {
            var plans = await _db.EmployerPlans.ToListAsync();
            return Ok(new { success = true, data = plans });
            }


        [Authorize(Roles = "Admin")]
        [HttpGet("admin/plans/{id}")]
        public async Task<IActionResult> GetPlan(int id)
            {
            var plan = await _db.EmployerPlans.FindAsync(id);

            if (plan == null)
                return NotFound(new { success = false, message = "Plan không tồn tại" });

            return Ok(new { success = true, data = plan });
            }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/plans")]
        public async Task<IActionResult> CreatePlan([FromBody] PaymentPlanDto dto)
            {
            var plan = new EmployerPlan
                {
                PlanName = dto.PlanName,
                Price = dto.Price,
                MaxPosts = dto.MaxPosts,
                DurationDays = dto.DurationDays,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
                };

            _db.EmployerPlans.Add(plan);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Tạo gói thành công", data = plan });
            }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/plans/{id}")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] PaymentPlanDto dto)
            {
            var plan = await _db.EmployerPlans.FindAsync(id);
            if (plan == null)
                return NotFound(new { success = false, message = "Plan không tồn tại" });

            plan.PlanName = dto.PlanName;
            plan.Price = dto.Price;
            plan.MaxPosts = dto.MaxPosts;
            plan.DurationDays = dto.DurationDays;
            plan.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật gói thành công", data = plan });
            }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/plans/{id}")]
        public async Task<IActionResult> DeletePlan(int id)
            {
            var plan = await _db.EmployerPlans.FindAsync(id);
            if (plan == null)
                return NotFound(new { success = false, message = "Plan không tồn tại" });

            // Kiểm tra có transaction nào gói này không
            bool inUse = await _db.EmployerTransactions.AnyAsync(x => x.PlanId == id);
            if (inUse)
                return BadRequest(new { success = false, message = "Không thể xóa gói vì đã có giao dịch sử dụng." });

            _db.EmployerPlans.Remove(plan);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa gói thành công" });
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
        }
    }
