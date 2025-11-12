using System.Collections.Generic;
using System.Threading.Tasks;
using PTJ_Models.DTO.Report;

namespace PTJ_Service.Interfaces
{
    public interface IReportService
    {
        Task<int> ReportEmployerPostAsync(int reporterId, CreateEmployerPostReportDto dto);
        Task<int> ReportJobSeekerPostAsync(int reporterId, CreateJobSeekerPostReportDto dto);
        Task<IEnumerable<MyReportDto>> GetMyReportsAsync(int reporterId);
    }
}
