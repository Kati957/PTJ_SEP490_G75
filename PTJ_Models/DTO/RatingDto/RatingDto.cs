using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.RatingDto
{
    public class RatingCreateDto
    {
        public int RateeId { get; set; }
        public int SubmissionId { get; set; }
        public decimal RatingValue { get; set; }
        public string? Comment { get; set; }
    }


    public class RatingViewDto
    {
        public int RatingId { get; set; }
        public int RaterId { get; set; }
        public string? RaterName { get; set; }
        public decimal RatingValue { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
