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
        var json = await _http.GetStringAsync("p/");
        return JsonConvert.DeserializeObject<List<VnPostProvince>>(json);
        }

    public async Task<List<VnPostDistrict>> GetDistrictsAsync(int provinceId)
        {
        var json = await _http.GetStringAsync($"p/{provinceId}?depth=2");
        var data = JsonConvert.DeserializeObject<ProvinceWithDistricts>(json);
        return data.districts;
        }

    public async Task<List<VnPostWard>> GetWardsAsync(int districtId)
        {
        var json = await _http.GetStringAsync($"d/{districtId}?depth=2");
        var data = JsonConvert.DeserializeObject<DistrictWithWards>(json);
        return data.wards;
        }
    }
