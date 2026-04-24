using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IHealthService
    {
        Task<IEnumerable<HealthRecord>> GetFilteredHealthRecordsAsync(int? childId, int centerId);
        Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByGuardianIdAsync(int guardianId);
        Task<HealthRecord?> GetHealthRecordByIdAsync(int healthId);
        Task<HealthRecord> CreateHealthRecordAsync(HealthRecord healthRecord);
        Task<bool> UpdateHealthRecordAsync(int healthId, HealthRecord healthRecord);
        Task<bool> DeleteHealthRecordAsync(int healthId);
    }
}
