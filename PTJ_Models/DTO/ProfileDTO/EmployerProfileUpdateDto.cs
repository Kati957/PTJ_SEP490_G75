using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO
    {
    public class EmployerProfileUpdateDto
        {
        [Required(ErrorMessage = "DisplayName is required")]
        [MaxLength(500, ErrorMessage = "DisplayName too long")]
        public string? DisplayName { get; set; }

        [MaxLength(1000, ErrorMessage = "Description too long")]
        public string? Description { get; set; }

        [MaxLength(500, ErrorMessage = "ContactName too long")]
        public string? ContactName { get; set; }

        [Required(ErrorMessage = "ContactPhone is required")]
        [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "Invalid ContactPhone")]
        public string? ContactPhone { get; set; }

        [Required(ErrorMessage = "ContactEmail is required")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string? ContactEmail { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid ProvinceId")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid DistrictId")]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid WardId")]
        public int WardId { get; set; }

        [MaxLength(500, ErrorMessage = "FullLocation too long")]
        public string? FullLocation { get; set; }

        [Url(ErrorMessage = "Invalid Website URL")]
        public string? Website { get; set; }

        public IFormFile? ImageFile { get; set; }
        }
    }
