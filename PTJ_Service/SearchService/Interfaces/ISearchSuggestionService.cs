namespace PTJ_Service.SearchService.Interfaces
{
    public interface ISearchSuggestionService
    {
        Task<IEnumerable<string>> GetSuggestionsAsync(string? keyword, int? roleId);
        Task<IEnumerable<string>> GetPopularKeywordsAsync(int? roleId);
    }
}
