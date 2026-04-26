using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IHealthService
    {
        Task<IEnumerable<HealthRecord>> GetFilteredHealthRecordsAsync(int? childId, int? centerId);
        Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByParentUserIdAsync(int parentUserId);
        Task<HealthRecord?> GetHealthRecordByIdAsync(int id);
        Task<HealthRecord> CreateHealthRecordAsync(CreateHealthDto dto, int recordedByUserId);
        Task<bool> UpdateHealthRecordAsync(int id, UpdateHealthDto dto);
        Task<bool> DeleteHealthRecordAsync(int id);
    }
}
