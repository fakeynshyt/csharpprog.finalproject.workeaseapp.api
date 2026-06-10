using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IReportService
    {
        Task<ReportListDto> GenerateMasterListAsync(GenerateMasterListDto dto, int userId);
        Task<ReportListDto> GeneratePdfSummaryAsync(GeneratePdfSummaryDto dto, int userId);
        Task<ReportListDto> GenerateReportFeeAsync(GenerateReportFeeDto dto, int userId);
        Task<ReportListDto> GenerateNarrativeAsync(GenerateNarrativeDto dto, int userId);

        Task<(byte[] file, string format, string title)> DownloadAsync(int reportId);
    }
}
