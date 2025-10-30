namespace PTJ_Models.DTO.SearchDTO
    {
    public class EmployerSearchFilterDto
        {
        public string? Keyword { get; set; }
        public int? CategoryID { get; set; }
        public string? Location { get; set; }
        public string? PreferredJobType { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }
        public string? Gender { get; set; }
        public decimal? MinExpectedSalary { get; set; }
        public decimal? MaxExpectedSalary { get; set; }
        }
    }

