using PTJ_Models.DTO.PostDTO;

public interface IEmployerPostService
{
    Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostCreateDto dto);

    Task<EmployerPostResultDto> RefreshSuggestionsAsync(
        int employerPostId,
        int? requesterId = null,
        bool isAdmin = false);

    Task SaveCandidateAsync(SaveCandidateDto dto);
    Task UnsaveCandidateAsync(SaveCandidateDto dto);
    Task<IEnumerable<object>> GetShortlistedByPostAsync(int employerPostId);

    Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync();
    Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId, bool isAdmin = false, bool isOwner = false);
    Task<EmployerPostDtoOut?> GetByIdAsync(
        int id,
        int? requesterId = null,
        bool isAdmin = false);

    Task<EmployerPostDtoOut?> UpdateAsync(
        int id,
        EmployerPostUpdateDto dto,
        int requesterId,
        bool isAdmin = false);

    Task<IEnumerable<EmployerPostSuggestionDto>> GetSuggestionsByPostAsync(
        int employerPostId,
        int take = 10,
        int skip = 0);

    Task<bool> DeleteAsync(int id);

    Task<string> CloseEmployerPostAsync(int id);
    Task<string> ReopenEmployerPostAsync(int id);

}
