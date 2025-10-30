using Microsoft.AspNetCore.Mvc;
using PTJ_Service.SearchService.Interfaces;

namespace PTJ_API.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchSuggestionController : ControllerBase
        {
        private readonly ISearchSuggestionService _suggestionService;

        public SearchSuggestionController(ISearchSuggestionService suggestionService)
            {
            _suggestionService = suggestionService;
            }

        
        // Gợi ý theo keyword + roleId
        
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? keyword, [FromQuery] int? roleId)
            {
            var suggestions = await _suggestionService.GetSuggestionsAsync(keyword, roleId);
            return Ok(suggestions);
            }

       
        // Từ khóa phổ biến theo roleId
        
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularKeywords([FromQuery] int? roleId)
            {
            var popular = await _suggestionService.GetPopularKeywordsAsync(roleId);
            return Ok(popular);
            }
        }
    }
