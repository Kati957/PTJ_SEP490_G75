namespace PTJ_Models.DTO.AdminDashbroad
{
    public class UserStatsByDayDto
    {
        public DateTime Date { get; set; }
        public int Employers { get; set; }
        public int JobSeekers { get; set; }
        public int Total => Employers + JobSeekers;
    }
    public class UserStatsByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Employers { get; set; }
        public int JobSeekers { get; set; }
        public int Total => Employers + JobSeekers;
        public string Label => $"{Month:00}/{Year}";
    }

    public class UserStatsByYearDto
    {
        public int Year { get; set; }
        public int Employers { get; set; }
        public int JobSeekers { get; set; }
        public int Total => Employers + JobSeekers;
    }
}
