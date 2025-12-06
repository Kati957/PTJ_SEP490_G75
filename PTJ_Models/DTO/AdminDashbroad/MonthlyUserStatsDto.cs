namespace PTJ_Models.DTO.AdminDashbroad
{
    public class MonthlyUserStatsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
        public string Label => $"{Month}/{Year}";
    }
}
