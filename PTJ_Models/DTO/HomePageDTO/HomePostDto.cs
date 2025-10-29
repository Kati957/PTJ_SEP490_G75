namespace PTJ_Models.DTO
{
    public class HomePostDto
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = string.Empty; // Employer | JobSeeker
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? CategoryName { get; set; }
        public decimal? Salary { get; set; }
        public string? WorkHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }  // Employer hoặc JobSeeker name
    }
}
