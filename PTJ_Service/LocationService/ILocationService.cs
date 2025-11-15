public interface ILocationService
    {
    Task<string?> GetProvinceName(int provinceId);
    Task<string?> GetDistrictName(int districtId, int provinceId);
    Task<string?> GetWardName(int wardId, int districtId);
    }
