namespace PTJ_Service.SearchService.Interfaces
    {
    public interface ISearchSuggestionService
        {
        Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword);

        // sửa lại — dùng role string, không dùng roleId nữa
        Task<IEnumerable<string>> GetPopularKeywordsAsync(string? role);
        }
    }
