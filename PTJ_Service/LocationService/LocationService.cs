using PTJ_Service.LocationService;

public class LocationService : ILocationService
    {
    private readonly LocationDisplayService _display;

    public LocationService(LocationDisplayService display)
        {
        _display = display;
        }

    public Task<string?> GetProvinceName(int id) => _display.GetProvinceName(id);
    public Task<string?> GetDistrictName(int id, int parentId) => _display.GetDistrictName(id, parentId);
    public Task<string?> GetWardName(int id, int parentId) => _display.GetWardName(id, parentId);
    }
