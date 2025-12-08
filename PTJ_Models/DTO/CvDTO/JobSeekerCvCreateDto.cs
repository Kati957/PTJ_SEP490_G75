using System;
using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.CvDTO
{
    public class JobSeekerCvCreateDto
    {
        [Required(ErrorMessage = "Please enter a Title")]
        public string CvTitle { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Skill summary too long")]
        public string? SkillSummary { get; set; }

        public string? Skills { get; set; }

        public string? PreferredJobType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select province/city")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select district")]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select ward/commune")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Please enter PhoneNumber")]
        [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "Invalid PhoneNumber")]
        public string? ContactPhone { get; set; }
    }
}
