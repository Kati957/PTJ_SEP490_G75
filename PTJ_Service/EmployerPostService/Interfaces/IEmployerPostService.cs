using PTJ_Models.DTO.PostDTO;

public interface IEmployerPostService
    {
    Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostCreateDto dto);
    Task<EmployerPostResultDto> RefreshSuggestionsAsync(int employerPostId);

    Task SaveCandidateAsync(SaveCandidateDto dto);
    Task UnsaveCandidateAsync(SaveCandidateDto dto);
    Task<IEnumerable<object>> GetShortlistedByPostAsync(int employerPostId);

    Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync();
    Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId);
    Task<EmployerPostDtoOut?> GetByIdAsync(int id);
    Task<EmployerPostDtoOut?> UpdateAsync(int id, EmployerPostUpdateDto dto);
    Task<IEnumerable<EmployerPostSuggestionDto>> GetSuggestionsByPostAsync(int employerPostId, int take = 10, int skip = 0);
    Task<bool> DeleteAsync(int id);

    Task<bool> CloseEmployerPostAsync(int id);
    Task<bool> ReopenEmployerPostAsync(int id);

    }
