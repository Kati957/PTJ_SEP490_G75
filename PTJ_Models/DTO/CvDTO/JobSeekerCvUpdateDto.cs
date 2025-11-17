using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.CvDTO
    {
    public class JobSeekerCvUpdateDto
        {
        [Required]
        public string CvTitle { get; set; } = null!;

        [MaxLength(1000)]
        public string? SkillSummary { get; set; }

        public string? Skills { get; set; }
        public string? PreferredJobType { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int WardId { get; set; }
        public string? ContactPhone { get; set; }
        }
    }
