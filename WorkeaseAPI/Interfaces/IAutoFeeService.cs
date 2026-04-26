namespace WorkeaseAPI.Interfaces
{
    public interface IAutoFeeService
    {
        Task GenerateFirstFeeAsync(int childId, int enrolledByUserId);
        Task GenerateMonthlyFeesAsync();
        Task ProcessOverdueFeesAsync();
    }
}
