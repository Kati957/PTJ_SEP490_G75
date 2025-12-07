namespace PTJ_Models.DTO.AdminDashbroad
{
    public class AdminDashboardSummaryDto
    {
        public int TotalUsers { get; set; }
        public int NewUsers30Days { get; set; }

        public int TotalEmployers { get; set; }
        public int TotalJobSeekers { get; set; }

        public int TotalPosts { get; set; }
        public int ActivePosts { get; set; }
        public int PendingPosts { get; set; }

        public int PendingReports { get; set; }
        public int SolvedReports { get; set; }

        public int TotalApplications { get; set; }
        public int NewApplications30Days { get; set; }
    }

}
