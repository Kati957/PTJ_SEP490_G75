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
        public string? Location { get; set; }
        public string? Website { get; set; }

        // 🖼️ file ảnh đại diện doanh nghiệp
        public IFormFile? ImageFile { get; set; }
        }
    }
