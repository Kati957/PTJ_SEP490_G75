using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.PostDTO;
using PTJ_Service.EmployerPostService;

namespace PTJ_API.Controllers.Post
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployerPostController : ControllerBase
    {
        private readonly IEmployerPostService _service;

        public EmployerPostController(IEmployerPostService service)
        {
            _service = service;
        }

        private IActionResult Forbidden(string message)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message });
        }

        private int? GetCurrentUserId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return sub != null ? int.Parse(sub.Value) : null;
        }


        // CREATE 

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] EmployerPostCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, errors = ModelState });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (!User.IsInRole("Admin") && dto.UserID != currentUserId)
                return Forbidden("Bạn không thể tạo bài thay người khác.");

            var result = await _service.CreateEmployerPostAsync(dto);
            return Ok(new { success = true, message = "Tạo bài đăng thành công.", data = result });
        }


        // GET ALL 

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(new { success = true, total = items.Count(), data = items });
        }


        // GET BY USER 

        [HttpGet("by-user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            bool isEmployer = User.IsInRole("Employer");
            bool isAdmin = User.IsInRole("Admin");

            if (isEmployer && currentUserId != userId)
                return Forbidden("Bạn chỉ được xem bài đăng của mình.");

            var items = await _service.GetByUserAsync(userId);

            if (!isAdmin && !User.IsInRole("Employer"))
                items = items.Where(x => x.Status == "Active");

            return Ok(new { success = true, total = items.Count(), data = items });
        }


        // GET BY ID 

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            var post = await _service.GetByIdAsync(id, currentUserId, isAdmin);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            return Ok(new { success = true, data = post });
        }


        // UPDATE 

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] EmployerPostUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, errors = ModelState });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            bool isAdmin = User.IsInRole("Admin");

            var existing = await _service.GetByIdAsync(id, currentUserId, isAdmin);
            if (existing == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            if (!isAdmin && existing.EmployerId != currentUserId)
                return Forbidden("Bạn không thể chỉnh sửa bài của người khác.");

            var updated = await _service.UpdateAsync(id, dto, currentUserId.Value, isAdmin);

            return Ok(new { success = true, message = "Cập nhật thành công.", data = updated });
        }


        // DELETE 

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            bool isAdmin = User.IsInRole("Admin");

            var post = await _service.GetByIdAsync(id, currentUserId, isAdmin);
            if (post == null)
                return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });

            if (!isAdmin && post.EmployerId != currentUserId)
                return Forbidden("Bạn không thể xóa bài của người khác.");

            var ok = await _service.DeleteAsync(id);
            return Ok(new { success = ok, message = ok ? "Xóa thành công" : "Xóa thất bại" });
        }


        // REFRESH AI 

        [HttpPost("refresh/{postId}")]
        public async Task<IActionResult> Refresh(int postId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            bool isAdmin = User.IsInRole("Admin");

            var post = await _service.GetByIdAsync(postId, currentUserId, isAdmin);
            if (post == null)
                return NotFound();

            if (!isAdmin && post.EmployerId != currentUserId)
                return Forbidden("Bạn không có quyền.");

            var data = await _service.RefreshSuggestionsAsync(postId, currentUserId, isAdmin);
            return Ok(new { success = true, data });
        }


        // SHORTLIST 

        [HttpPost("save-candidate")]
        public async Task<IActionResult> SaveCandidate([FromBody] SaveCandidateDto dto)
        {
            await _service.SaveCandidateAsync(dto);
            return Ok(new { success = true });
        }

        [HttpPost("unsave-candidate")]
        public async Task<IActionResult> UnsaveCandidate([FromBody] SaveCandidateDto dto)
        {
            await _service.UnsaveCandidateAsync(dto);
            return Ok(new { success = true });
        }

        [HttpGet("shortlist/{postId}")]
        public async Task<IActionResult> GetShortlisted(int postId)
        {
            var items = await _service.GetShortlistedByPostAsync(postId);
            return Ok(new { success = true, total = items.Count(), data = items });
        }


        // SUGGESTIONS 

        [HttpGet("{postId}/suggestions")]
        public async Task<IActionResult> GetSuggestions(int postId, [FromQuery] int take = 10, [FromQuery] int skip = 0)
        {
            var items = await _service.GetSuggestionsByPostAsync(postId, take, skip);
            return Ok(new { success = true, total = items.Count(), data = items });
        }


        // CLOSE / REOPEN 

        [HttpPut("{id}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var message = await _service.CloseEmployerPostAsync(id);
            bool success = message.StartsWith("Đã");

            return Ok(new { success, message });
        }

        [HttpPut("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var message = await _service.ReopenEmployerPostAsync(id);
            bool success = message.StartsWith("Đã");

            return Ok(new { success, message });
        }
    }
}
