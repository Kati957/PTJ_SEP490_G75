using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PTJ_Data;
using PTJ_Service.EmployerPostService.Implementations;
using PTJ_Models.DTO.FilterDTO;
using PTJ_Service.JobSeekerPostService.Implementations;
using PTJ_Models.Models;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;

namespace YourProject.Controllers
    {
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
        {
        private readonly JobMatchingDbContext _db;
        private readonly IJobSeekerPostService _seekerService;

        public MatchController(JobMatchingDbContext db, IJobSeekerPostService seekerService)
        {
            _seekerService = seekerService;
            _db = db;
        }

        [HttpGet("search-by-province")]
        public async Task<IActionResult> SearchByProvince(int provinceId)
            {
            if (provinceId <= 0)
                return BadRequest("ProvinceId không hợp lệ");


            // 1️⃣ KIỂM TRA USER ĐĂNG NHẬP?

            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                }

            // Default cho khách (guest)
            string userRole = "JobSeeker";

            // Nếu đã login → lấy role từ DB
            if (userId != null)
                {
                var user = await _db.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return NotFound("Không tìm thấy user.");

                if (user.Roles.Any(r => r.RoleName == "Admin"))
                    userRole = "Admin";
                else if (user.Roles.Any(r => r.RoleName == "Employer"))
                    userRole = "Employer";
                else if (user.Roles.Any(r => r.RoleName == "JobSeeker"))
                    userRole = "JobSeeker"; // đúng tên trong DB
                }


            // 2️⃣ ADMIN → Trả về toàn bộ dữ liệu trong tỉnh

            if (userRole == "Admin")
                {
                var employers = await _db.EmployerPosts
                    .Where(e => e.ProvinceId == provinceId)
                    .ToListAsync();

                var seekers = await _db.JobSeekerPosts
                    .Where(s => s.ProvinceId == provinceId)
                    .ToListAsync();

                return Ok(new
                    {
                    Employers = employers,
                    JobSeekers = seekers
                    });
                }


            // 3️⃣ JOB SEEKER (hoặc KHÁCH) → Xem EmployerPosts

            if (userRole == "JobSeeker")
                {
                var employers = await _db.EmployerPosts
                    .Where(e => e.ProvinceId == provinceId)
                    .ToListAsync();

                return Ok(employers);
                }


            // 4️⃣ EMPLOYER → Xem JobSeekerPosts

            if (userRole == "Employer")
                {
                var seekers = await _db.JobSeekerPosts
                    .Where(s => s.ProvinceId == provinceId)
                    .ToListAsync();

                return Ok(seekers);
                }

            return BadRequest("Role không hợp lệ.");
            }

        [HttpPost("search-by-salary")]
        public async Task<IActionResult> SearchBySalary([FromBody] SalarySearchDto dto)
            {
            var query = _db.EmployerPosts
                .Include(x => x.Category)
                .Include(x => x.User)
                .Where(x => x.Status == "Active")
                .AsQueryable();

            // 1️⃣ Nếu chọn "Thỏa thuận"
            if (dto.Negotiable)
                {
                query = query.Where(x => x.SalaryMin == null && x.SalaryMax == null);

                var negotiableJobs = await query.Select(x => new {
                    x.EmployerPostId,
                    x.Title,
                    x.Description,
                    SalaryText = "Thỏa thuận",
                    CategoryName = x.Category.Name,
                    EmployerName = x.User.Username,
                    x.Location,
                    x.CreatedAt
                    }).ToListAsync();

                return Ok(negotiableJobs);
                }

            // 2️⃣ Lọc theo MinSalary
            if (dto.MinSalary.HasValue)
                {
                query = query.Where(x =>
                    (x.SalaryMin.HasValue && x.SalaryMin >= dto.MinSalary.Value) ||
                    (x.SalaryMax.HasValue && x.SalaryMax >= dto.MinSalary.Value)
                );
                }

            // 3️⃣ Lọc theo MaxSalary
            if (dto.MaxSalary.HasValue)
                {
                query = query.Where(x =>
                    (x.SalaryMax.HasValue && x.SalaryMax <= dto.MaxSalary.Value) ||
                    (x.SalaryMin.HasValue && x.SalaryMin <= dto.MaxSalary.Value)
                );
                }

            // 4️⃣ Bao gồm job "thỏa thuận" hay không?
            if (!dto.IncludeNegotiable)
                {
                query = query.Where(x => x.SalaryMin != null || x.SalaryMax != null);
                }

            // 5️⃣ Trả kết quả
            var result = await query.Select(x => new {
                x.EmployerPostId,
                x.Title,
                x.Description,
                SalaryRange =
                    (x.SalaryMin == null && x.SalaryMax == null)
                        ? "Thỏa thuận"
                        : $"{(x.SalaryMin ?? 0):#,###} - {(x.SalaryMax ?? 0):#,###}",
                CategoryName = x.Category.Name,
                EmployerName = x.User.Username,
                x.Location,
                x.CreatedAt
                }).ToListAsync();

            return Ok(result);
            }

        }
    }
