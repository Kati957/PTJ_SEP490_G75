using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO;
using PTJ_Service.JobSeekerPostService;

namespace PTJ_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSeekerPostController : ControllerBase
    {
        private readonly IJobSeekerPostService _service;

        public JobSeekerPostController(IJobSeekerPostService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] JobSeekerPostDto dto)
        {
            var result = await _service.CreateJobSeekerPostAsync(dto);
            return Ok(new
            {
                post = result.Post,
                suggestions = result.SuggestedJobs
            });
        }



        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _service.GetAllAsync();
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _service.GetByIdAsync(id);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
