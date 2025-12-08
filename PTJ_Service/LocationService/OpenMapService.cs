using Newtonsoft.Json;
using PTJ_Models.Models;
using System.Globalization;
using PTJ_Data;
using PTJ_Models;

namespace PTJ_Service.LocationService
{
    public class OpenMapService
    {
        private readonly HttpClient _http;
        private readonly JobMatchingDbContext _db;

        public OpenMapService(JobMatchingDbContext db)
        {
            _db = db;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("JobMatchingAI/1.0 (contact: support@yourdomain.com)");
        }

        //  Lấy tọa độ từ địa chỉ (có cache DB)
        public virtual async Task<(double lat, double lng)?> GetCoordinatesAsync(string address)
            {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            // cache phải theo FULL ADDRESS
            var cached = _db.LocationCaches.FirstOrDefault(x => x.Address == address);
            if (cached != null)
                return (cached.Lat, cached.Lng);

            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
            var response = await _http.GetStringAsync(url);
            var json = JsonConvert.DeserializeObject<dynamic>(response);

            if (json == null || json.Count == 0)
                return null;

            double lat = double.Parse((string)json[0].lat, CultureInfo.InvariantCulture);
            double lng = double.Parse((string)json[0].lon, CultureInfo.InvariantCulture);

            // lưu đầy đủ, KHÔNG rút gọn
            _db.LocationCaches.Add(new LocationCache
                {
                Address = address,
                Lat = lat,
                Lng = lng,
                LastUpdated = DateTime.Now
                });

            await _db.SaveChangesAsync();

            return (lat, lng);
            }

        //  Tính khoảng cách giữa 2 tọa độ (km)
        public virtual double ComputeDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // bán kính Trái Đất km
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));
            return R * c;
        }
        }
    }
