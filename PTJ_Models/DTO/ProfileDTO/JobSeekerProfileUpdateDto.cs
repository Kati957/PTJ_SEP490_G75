using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO
    {
    public class JobSeekerProfileUpdateDto
        {
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }

        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int WardId { get; set; }

        public string? ContactPhone { get; set; }

        public IFormFile? ImageFile { get; set; }
        }
    }
