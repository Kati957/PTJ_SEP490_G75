using PTJ_Models.DTO;

public interface IReportService
{
    Task<int> ReportPostAsync(int reporterId, CreatePostReportDto dto);
    Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId);
}
