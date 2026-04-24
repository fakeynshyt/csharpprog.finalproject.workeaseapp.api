using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface ISyncService
    {
        Task<InitialPackageDto> GetInitialPackageAsync(int userId, string role);
        Task<SyncResultDto> ProcessSyncAsync(SyncPayloadDto payload);
    }
}
