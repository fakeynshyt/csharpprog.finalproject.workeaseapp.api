using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IFeeService
    {
        Task<IEnumerable<FeeRecord>> GetFilteredFeeRecordAsync(int? centerId, int? month, int? year);
        Task<IEnumerable<FeeSummaryDto>> GetFeeRecordByGuardianUserIdAsync(int parentUserId);
        Task<FeeRecord?> GetFeeRecordByIdAsync(int id);
        Task<FeeRecord> CreateFeeRecordAsync(CreateFeeDto dto, int recordedByUserId);
        Task<bool> MarkFeeRecordAsPaidAsync(int id);
        Task<bool> UpdateFeeRecordAsync(int id, UpdateFeeDto dto);
        Task<bool> DeleteFeeRecordAsync(int id);
    }
}
