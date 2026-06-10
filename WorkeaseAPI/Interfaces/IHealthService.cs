using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IHealthService
    {

        
        Task<IEnumerable<HealthSummaryDto>> GetFilteredHealthRecordsAsync(int? childId, int? centerId);
        Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByGuardianIdAsync(int parentUserId, int? childId, int? month, int? year);
        Task<HealthSummaryDto?> GetHealthRecordByIdAsync(int id);
        Task<HealthSummaryDto> CreateHealthRecordAsync(CreateHealthDto dto, int recordedByUserId);
        Task<bool> UpdateHealthRecordAsync(int id, UpdateHealthDto dto);
        Task<bool> DeleteHealthRecordAsync(int id);
        Task<IEnumerable<AbnormalBmiDto>> GetAbnormalChildrenBmiAsync();
    }
}
