public interface ISearchSuggestionService
    {
    Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword);
    Task<IEnumerable<string>> GetPopularKeywordsAsync(string? role);
    }
