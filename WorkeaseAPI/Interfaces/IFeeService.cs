using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IFeeService
    {
        Task<IEnumerable<FeeRecord>> GetFilteredFeeRecordsAsync(int? centerId, int? month, int? year);
        Task<IEnumerable<FeeSummaryDto>> GetFeeRecordsByGuardianId(int guardianId);
        Task<FeeRecord?> GetFeeRecordById(int id);
        Task<FeeRecord> CreateFeeRecord(FeeRecord feeRecord);
        Task<bool> UpdateFeeRecordAsync(int feeId, FeeRecord feeRecord);
        Task<bool> DeleteFeeRecordAsync(int feeId);
    }
}
