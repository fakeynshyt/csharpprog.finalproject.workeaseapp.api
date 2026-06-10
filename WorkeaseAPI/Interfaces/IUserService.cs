using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<CdwUserDto?>> GetAllUsersAsync();
        Task<CdwUserDto?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(CreateUserDto dto);
        Task<bool> AdminUpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
    }
}
