using PTJ_Models.DTO;

namespace PTJ_Service.Interfaces
{
    public interface IReportService
    {
        Task<int> ReportPostAsync(int reporterId, CreatePostReportDto dto);
        Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId);
    }
}
