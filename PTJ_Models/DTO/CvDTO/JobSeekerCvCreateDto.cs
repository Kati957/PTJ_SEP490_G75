using System;
using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.CvDTO
    {
    public class JobSeekerCvCreateDto
        {
        [Required]
        public string CvTitle { get; set; } = null!;

        [MaxLength(1000)]
        public string? SkillSummary { get; set; }

        public string? Skills { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }
        public string? ContactPhone { get; set; }
        }
    }
