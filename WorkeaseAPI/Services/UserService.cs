using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        public UserService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<bool> AdminUpdateUserAsync(int id, User updatedUser)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            user.UserName = updatedUser.UserName;
            user.UserEmail = updatedUser.UserEmail;
            user.UserType = updatedUser.UserType;
            user.CenterId = updatedUser.CenterId;
            user.UserIsActive = updatedUser.UserIsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<User> CreateUserAsync(User user, string plainPassword)
        {
            user.UserHashPassword = AuthenticationService.HashPassword(plainPassword);
            user.UserEnrolledAt = DateTime.UtcNow;

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            user.UserIsActive = false;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<User?>> GetAllUsersAsync() =>
            await _db.Users
                .Include(u => u.Center)
                .Where(u => u.UserIsActive)
                .OrderBy(u => u.UserType)
                .ThenBy(u => u.UserName)
                .ToListAsync();



        public async Task<User?> GetUserByIdAsync(int id) => 
            await _db.Users
                .Include(u => u.Center)
                .FirstOrDefaultAsync(u => u.UserId == id && u.UserIsActive);
    }
}
