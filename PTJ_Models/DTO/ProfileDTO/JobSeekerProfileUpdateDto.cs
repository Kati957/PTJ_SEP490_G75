using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO
    {
    public class JobSeekerProfileUpdateDto
        {
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }
        public string? ContactPhone { get; set; }

        // 🖼️ file ảnh người dùng chọn để tải lên
        public IFormFile? ImageFile { get; set; }
        }
    }
