using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IReportService
    {
        Task<ReportSummaryDto> GenerateMonthlyAsync(int cdwUserId,
                                                GenerateReportRequest request);
        Task<(byte[] file, string format)> DownloadAsync(int reportId, int cdwUserId);
        Task<IEnumerable<ReportSummaryDto>> GetMyReportsAsync(int cdwUserId);
    }
}
