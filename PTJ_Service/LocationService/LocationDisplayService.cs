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
            if (provinceId <= 0)
                return "Chưa chọn tỉnh";

            var provinces = await _vnpost.GetProvincesAsync();
            return provinces.FirstOrDefault(p => p.code == provinceId)?.name ?? "Không tìm thấy tỉnh";
            }

        public async Task<string?> GetDistrictName(int districtId, int provinceId)
            {
            if (provinceId <= 0 || districtId <= 0)
                return "Chưa chọn quận/huyện";

            var districts = await _vnpost.GetDistrictsAsync(provinceId);
            return districts.FirstOrDefault(d => d.code == districtId)?.name ?? "Không tìm thấy quận/huyện";
            }

        public async Task<string?> GetWardName(int wardId, int districtId)
            {
            if (districtId <= 0 || wardId <= 0)
                return "Chưa chọn phường/xã";

            var wards = await _vnpost.GetWardsAsync(districtId);
            return wards.FirstOrDefault(w => w.code == wardId)?.name ?? "Không tìm thấy phường/xã";
            }

        public virtual async Task<string> BuildAddressAsync(int provinceId, int districtId, int wardId)
            {
            // Nếu cả 3 = 0 → chưa cập nhật
            if (provinceId == 0 && districtId == 0 && wardId == 0)
                return "Chưa cập nhật địa chỉ";

            var province = await GetProvinceName(provinceId);
            var district = await GetDistrictName(districtId, provinceId);
            var ward = await GetWardName(wardId, districtId);

            return $"{ward}, {district}, {province}";
            }
        }
    }
