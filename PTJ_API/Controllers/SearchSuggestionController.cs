using Microsoft.AspNetCore.Mvc;
using PTJ_Service.SearchService;

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

        /// <summary>
        /// Gợi ý theo keyword + roleId
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? keyword, [FromQuery] int? roleId)
            {
            var suggestions = await _suggestionService.GetSuggestionsAsync(keyword, roleId);
            return Ok(suggestions);
            }

        /// <summary>
        /// Từ khóa phổ biến theo roleId
        /// </summary>
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularKeywords([FromQuery] int? roleId)
            {
            var popular = await _suggestionService.GetPopularKeywordsAsync(roleId);
            return Ok(popular);
            }
        }
    }
