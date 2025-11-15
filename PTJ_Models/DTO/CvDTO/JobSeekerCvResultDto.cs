using System;

namespace PTJ_Models.DTO.CvDTO
    {
    public class JobSeekerCvResultDto
        {
        public int Cvid { get; set; }
        public int JobSeekerId { get; set; }
        public string CvTitle { get; set; }
        public string SkillSummary { get; set; }
        public string Skills { get; set; }
        public string PreferredJobType { get; set; }

        public string PreferredLocationName { get; set; }

        public string ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        }

    }
