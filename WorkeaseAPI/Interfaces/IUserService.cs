using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User?>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(CreateUserDto dto);
        Task<bool> AdminUpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
    }
}
