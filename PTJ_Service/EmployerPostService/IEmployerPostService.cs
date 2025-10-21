using PTJ_Models.DTO;

public interface IEmployerPostService
{
    Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostDto dto);
    Task<EmployerPostResultDto> RefreshSuggestionsAsync(int employerPostId);

    Task SaveCandidateAsync(SaveCandidateDto dto);
    Task UnsaveCandidateAsync(SaveCandidateDto dto);
    Task<IEnumerable<object>> GetShortlistedByPostAsync(int employerPostId);

    Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync();
    Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId);
    Task<EmployerPostDtoOut?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}
