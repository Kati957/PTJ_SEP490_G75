using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO
    {
    public class JobSeekerProfileUpdateDto
        {
        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(200, ErrorMessage = "FullName too long")]
        public string? FullName { get; set; }

        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Invalid Gender")]
        public string? Gender { get; set; }

        [Range(1900, 2025, ErrorMessage = "Invalid BirthYear")]
        public int? BirthYear { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid ProvinceId")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid DistrictId")]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid WardId")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "ContactPhone required")]
        [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "Invalid ContactPhone")]
        public string? ContactPhone { get; set; }

        [MaxLength(500, ErrorMessage = "FullLocation too long")]
        public string? FullLocation { get; set; }

        public IFormFile? ImageFile { get; set; }
        }
    }
