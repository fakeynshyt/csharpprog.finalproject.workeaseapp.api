using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User?>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(User user, string plainPassword);
        Task<bool> AdminUpdateUserAsync(int id, User updatedUser);
        Task<bool> DeleteUserAsync(int id);
    }
}
