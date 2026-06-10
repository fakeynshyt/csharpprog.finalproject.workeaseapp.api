using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IFeeService
    {
        Task<IEnumerable<FeeSummaryDto>> GetFilteredFeeRecordAsync(int? childId, int? centerId, string? receiptNo);
        Task<IEnumerable<FeeSummaryDto>> GetFeeRecordByGuardianUserIdAsync(int parentUserId, int? childId, int? month, int? year);
        Task<FeeCalculatedDto> GetCalculatedFeeByChildAsync(int childId);
        Task<FeeSummaryDto?> GetFeeRecordByIdAsync(int id);
        Task<FeeSummaryDto> CreateFeeRecordAsync(CreateFeeDto dto, int recordedByUserId);
        Task<bool> MarkFeeRecordAsPaidAsync(int id);
        Task<bool> UpdateFeeRecordAsync(int id, UpdateFeeDto dto);
        Task<bool> DeleteFeeRecordAsync(int id);


        Task<FeesSummaryDto> GetOverallFeesSummaryAsync(int? centerId, int? month, int? year);
    }
}
