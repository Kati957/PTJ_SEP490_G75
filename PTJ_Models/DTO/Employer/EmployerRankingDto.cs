namespace PTJ_Models.DTO.Employer
{
    public class EmployerRankingDto
    {
        public int EmployerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public int TotalApplyCount { get; set; }
        public int ActivePostCount { get; set; }
    }

}
