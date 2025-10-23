using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.JobSeekerPostService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSeekerPostController : ControllerBase
    {
        private readonly IJobSeekerPostService _jobSeekerPostService;

        public JobSeekerPostController(IJobSeekerPostService jobSeekerPostService)
        {
            _jobSeekerPostService = jobSeekerPostService;
        }

        // =========================================================
        // 🧠 TẠO BÀI ĐĂNG ỨNG VIÊN (TỰ GỢI Ý VIỆC LÀM BẰNG AI)
        // =========================================================
        /// <summary>
        /// Tạo mới bài đăng tìm việc của ứng viên, và trả về danh sách gợi ý việc làm.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateJobSeekerPost([FromBody] JobSeekerPostDto dto)
        {
            try
            {
                var result = await _jobSeekerPostService.CreateJobSeekerPostAsync(dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi tạo bài đăng: " + ex.Message,
                    inner = ex.InnerException?.Message
                });
            }

        }

        // =========================================================
        // 🔁 LÀM MỚI GỢI Ý VIỆC LÀM (AI)
        // =========================================================
        /// <summary>
        /// Làm mới gợi ý việc làm cho bài đăng ứng viên hiện tại.
        /// </summary>
        [HttpPut("refresh/{postId}")]
        public async Task<IActionResult> RefreshSuggestions(int postId)
        {
            try
            {
                var result = await _jobSeekerPostService.RefreshSuggestionsAsync(postId);
                return Ok(new
                {
                    success = true,
                    message = "Đã làm mới gợi ý việc làm.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Lỗi khi làm mới: {ex.Message}"
                });
            }
        }

        // =========================================================
        // 📋 LẤY DANH SÁCH BÀI ĐĂNG ỨNG VIÊN
        // =========================================================
        /// <summary>
        /// Lấy toàn bộ bài đăng ứng viên (Admin/Employer xem).
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _jobSeekerPostService.GetAllAsync();
            return Ok(new { success = true, data = posts });
        }

        /// <summary>
        /// Lấy bài đăng theo UserID.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var posts = await _jobSeekerPostService.GetByUserAsync(userId);
            return Ok(new { success = true, data = posts });
        }

        /// <summary>
        /// Lấy chi tiết bài đăng theo ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _jobSeekerPostService.GetByIdAsync(id);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            return Ok(new { success = true, data = post });
        }

        // =========================================================
        // 🗑️ XOÁ BÀI ĐĂNG (SOFT DELETE)
        // =========================================================
        /// <summary>
        /// Xóa mềm 1 bài đăng ứng viên (Status = "Deleted").
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _jobSeekerPostService.DeleteAsync(id);
            if (!ok)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            return Ok(new { success = true, message = "Đã xóa bài đăng." });
        }

        // =========================================================
        // ⭐ DANH SÁCH VIỆC YÊU THÍCH (SHORTLIST)
        // =========================================================
        /// <summary>
        /// Lưu việc làm yêu thích của ứng viên.
        /// </summary>
        [HttpPost("save-job")]
        public async Task<IActionResult> SaveJob([FromBody] SaveJobDto dto)
        {
            try
            {
                await _jobSeekerPostService.SaveJobAsync(dto);
                return Ok(new { success = true, message = "Đã lưu việc làm yêu thích." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Bỏ lưu việc làm khỏi danh sách yêu thích.
        /// </summary>
        [HttpPost("unsave-job")]
        public async Task<IActionResult> UnsaveJob([FromBody] SaveJobDto dto)
        {
            try
            {
                await _jobSeekerPostService.UnsaveJobAsync(dto);
                return Ok(new { success = true, message = "Đã bỏ lưu việc làm." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách việc làm đã lưu của ứng viên.
        /// </summary>
        [HttpGet("saved-jobs/{jobSeekerId}")]
        public async Task<IActionResult> GetSavedJobs(int jobSeekerId)
        {
            var jobs = await _jobSeekerPostService.GetSavedJobsAsync(jobSeekerId);
            return Ok(new { success = true, data = jobs });
        }
    }
}
