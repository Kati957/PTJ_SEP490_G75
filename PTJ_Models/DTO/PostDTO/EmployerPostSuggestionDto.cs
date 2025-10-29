using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PostDTO
    {
    public class EmployerPostSuggestionDto
        {
        public int JobSeekerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PreferredLocation { get; set; }
        public string? PreferredWorkHours { get; set; }
        public string SeekerName { get; set; } = string.Empty;
        public int MatchPercent { get; set; }
        public double RawScore { get; set; }
        public bool IsSaved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        }
    }
