using Microsoft.AspNetCore.Mvc;
using PTJ_Service.LocationService;

namespace PTJ_API.Controllers
    {
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
        {
        private readonly VnProstLocationService _location;

        public LocationController(VnProstLocationService location)
            {
            _location = location;
            }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
            {
            return Ok(await _location.GetProvincesAsync());
            }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
            {
            return Ok(await _location.GetDistrictsAsync(provinceId));
            }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
            {
            return Ok(await _location.GetWardsAsync(districtId));
            }
        }
    }
