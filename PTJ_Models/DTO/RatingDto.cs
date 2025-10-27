namespace PTJ_Models.DTOs
{
    public class RatingCreateDto
    {
        public int RaterId { get; set; }       // Người đánh giá
        public int RateeId { get; set; }       // Người được đánh giá
        public int? SubmissionId { get; set; } // Liên quan đến job nào
        public decimal RatingValue { get; set; } // 0–5
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
