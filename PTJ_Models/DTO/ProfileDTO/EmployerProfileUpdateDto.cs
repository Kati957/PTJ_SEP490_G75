    using Microsoft.AspNetCore.Http;

    namespace PTJ_Models.DTO
        {
        public class EmployerProfileUpdateDto
            {
            public string? DisplayName { get; set; }
            public string? Description { get; set; }
            public string? ContactName { get; set; }
            public string? ContactPhone { get; set; }
            public string? ContactEmail { get; set; }
            public int ProvinceId { get; set; }
            public int DistrictId { get; set; }
            public int WardId { get; set; }

            public string? FullLocation { get; set; }
            public string? Website { get; set; }

            public IFormFile? ImageFile { get; set; }
            }
        }
