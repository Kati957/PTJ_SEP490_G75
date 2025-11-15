using Newtonsoft.Json;
using PTJ_Service.LocationService.Models;

public class VnPostLocationService
    {
    private readonly HttpClient _http;

    public VnPostLocationService(HttpClient http)
        {
        _http = http;
        _http.BaseAddress = new Uri("https://provinces.open-api.vn/api/");
        }

    public async Task<List<VnPostProvince>> GetProvincesAsync()
        {
        try
            {
            var json = await _http.GetStringAsync("p/");
            return JsonConvert.DeserializeObject<List<VnPostProvince>>(json) ?? new();
            }
        catch
            {
            return new();
            }
        }

    public async Task<List<VnPostDistrict>> GetDistrictsAsync(int provinceId)
        {
        if (provinceId <= 0)
            return new();

        try
            {
            var json = await _http.GetStringAsync($"p/{provinceId}?depth=2");
            var data = JsonConvert.DeserializeObject<ProvinceWithDistricts>(json);
            return data?.districts ?? new();
            }
        catch
            {
            return new();
            }
        }

    public async Task<List<VnPostWard>> GetWardsAsync(int districtId)
        {
        if (districtId <= 0)
            return new();

        try
            {
            var json = await _http.GetStringAsync($"d/{districtId}?depth=2");
            var data = JsonConvert.DeserializeObject<DistrictWithWards>(json);
            return data?.wards ?? new();
            }
        catch
            {
            return new();
            }
        }
    }
