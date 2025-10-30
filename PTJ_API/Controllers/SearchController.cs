using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.SearchDTO;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
        {
        private readonly IEmployerSearchService _employerSearch;
        private readonly IJobSeekerSearchService _jobSeekerSearch;

        public SearchController(IEmployerSearchService employerSearch, IJobSeekerSearchService jobSeekerSearch)
            {
            _employerSearch = employerSearch;
            _jobSeekerSearch = jobSeekerSearch;
            }

        //  Employer tìm JobSeeker
        [HttpPost("jobseekers")]
        public async Task<IActionResult> SearchJobSeekers([FromBody] EmployerSearchFilterDto filter)
            {
            var result = await _employerSearch.SearchJobSeekersAsync(filter);
            return Ok(result);
            }

        //  JobSeeker tìm EmployerPost
        [HttpPost("employerposts")]
        public async Task<IActionResult> SearchEmployerPosts([FromBody] JobSeekerSearchFilterDto filter)
            {
            var result = await _jobSeekerSearch.SearchEmployerPostsAsync(filter);
            return Ok(result);
            }
        }
    }
