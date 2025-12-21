using System.Threading.Tasks;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Service.LocationService;

namespace PTJ_Service.Tests.CreateEmployerPost
    {
    /// <summary>
    /// Fake OpenMapService để tránh gọi API thật trong unit test.
    /// </summary>
    public class FakeOpenMapService : OpenMapService
        {
        public FakeOpenMapService(JobMatchingOpenAiDbContext db) : base(db)
            {
            }

        /// <summary>
        /// Luôn trả về toạ độ cố định.
        /// </summary>
        public override Task<(double lat, double lng)?> GetCoordinatesAsync(string address)
            {
            return Task.FromResult<(double, double)?>((10.0, 20.0));
            }

        /// <summary>
        /// Luôn coi khoảng cách là 10km (<=100km).
        /// </summary>
        public override double ComputeDistanceKm(double lat1, double lng1, double lat2, double lng2)
            {
            return 10;
            }
        }
    }
