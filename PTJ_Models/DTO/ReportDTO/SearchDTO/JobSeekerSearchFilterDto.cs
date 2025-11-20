namespace PTJ_Models.DTO.ReportDTO.SearchDTO
    {
    public class JobSeekerSearchFilterDto
        {
        public string? Keyword { get; set; }
        public int? CategoryID { get; set; }
        public string? Location { get; set; }
        public string? WorkHours { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        }
    }
