using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Service.Admin.Interfaces;

public class AdminJobPostService : IAdminJobPostService
    {
    private readonly IAdminJobPostRepository _repo;
    private readonly ILocationService _location;

    public AdminJobPostService(IAdminJobPostRepository repo, ILocationService location)
        {
        _repo = repo;
        _location = location;
        }

    public Task<PagedResult<AdminEmployerPostDto>> GetEmployerPostsAsync(
        string? status, int? categoryId, string? keyword, int page, int pageSize)
        => _repo.GetEmployerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

    public async Task<AdminEmployerPostDetailDto?> GetEmployerPostDetailAsync(int id)
        {
        var dto = await _repo.GetEmployerPostDetailAsync(id);
        if (dto == null) return null;

        dto.ProvinceName = await _location.GetProvinceName(dto.ProvinceId);
        dto.DistrictName = await _location.GetDistrictName(dto.DistrictId, dto.ProvinceId);
        dto.WardName = await _location.GetWardName(dto.WardId, dto.DistrictId);

        return dto;
        }

    public async Task ToggleEmployerPostBlockedAsync(int id)
        {
        var ok = await _repo.ToggleEmployerPostBlockedAsync(id);
        if (!ok) throw new KeyNotFoundException("Employer post not found.");
        }

    public Task<PagedResult<AdminJobSeekerPostDto>> GetJobSeekerPostsAsync(
        string? status, int? categoryId, string? keyword, int page, int pageSize)
        => _repo.GetJobSeekerPostsPagedAsync(status, categoryId, keyword, page, pageSize);

    public async Task<AdminJobSeekerPostDetailDto?> GetJobSeekerPostDetailAsync(int id)
        {
        var dto = await _repo.GetJobSeekerPostDetailAsync(id);
        if (dto == null) return null;

        dto.ProvinceName = await _location.GetProvinceName(dto.ProvinceId);
        dto.DistrictName = await _location.GetDistrictName(dto.DistrictId, dto.ProvinceId);
        dto.WardName = await _location.GetWardName(dto.WardId, dto.DistrictId);

        return dto;
        }

    public async Task ToggleJobSeekerPostArchivedAsync(int id)
        {
        var ok = await _repo.ToggleJobSeekerPostArchivedAsync(id);
        if (!ok) throw new KeyNotFoundException("JobSeeker post not found.");
        }
    }
