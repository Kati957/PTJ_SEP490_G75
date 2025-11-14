namespace PTJ_Service.LocationService
    {
    public class LocationDisplayService
        {
        private readonly VnPostLocationService _vnpost;

        public LocationDisplayService(VnPostLocationService vnpost)
            {
            _vnpost = vnpost;
            }

        public async Task<string?> GetProvinceName(int provinceId)
            {
            var provinces = await _vnpost.GetProvincesAsync();
            return provinces.FirstOrDefault(p => p.code == provinceId)?.name;
            }

        public async Task<string?> GetDistrictName(int districtId, int provinceId)
            {
            var districts = await _vnpost.GetDistrictsAsync(provinceId);
            return districts.FirstOrDefault(d => d.code == districtId)?.name;
            }

        public async Task<string?> GetWardName(int wardId, int districtId)
            {
            var wards = await _vnpost.GetWardsAsync(districtId);
            return wards.FirstOrDefault(w => w.code == wardId)?.name;
            }

        public async Task<string> BuildAddressAsync(int provinceId, int districtId, int wardId)
            {
            var province = await GetProvinceName(provinceId);
            var district = await GetDistrictName(districtId, provinceId);
            var ward = await GetWardName(wardId, districtId);

            if (province == null || district == null || ward == null)
                return "Địa chỉ không hợp lệ";

            return $"{ward}, {district}, {province}";
            }
        }
    }
