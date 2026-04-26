using DocumentFormat.OpenXml.Math;
using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Helpers;
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
        public async Task<bool> AdminUpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await GetUserByIdAsync(id);
            if (user is null) return false;

            var emailConflict = await _db.Users
                                         .AnyAsync(u => u.UserEmail == dto.UserEmail
                                                     && u.UserId != id);
            if (emailConflict)
                throw new Exception("Email is already used by another account.");

            if (dto.CenterId == 0)
                dto.CenterId = null;

            if (dto.UserType == "CDW")
            {
                if (dto.CenterId is null)
                    throw new Exception("CDW workers must be assigned to a center.");

                var centerExists = await _db.Centers
                                            .AnyAsync(c => c.CenterId == dto.CenterId);
                if (!centerExists)
                    throw new Exception($"Center with ID {dto.CenterId} not found.");
            }

            if (dto.UserType != "CDW")
                dto.CenterId = null;

            user.UserName = dto.UserName;
            user.UserEmail = dto.UserEmail;
            user.UserType = dto.UserType;
            user.CenterId = dto.CenterId;
            user.UserIsActive = dto.UserIsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<User> CreateUserAsync(CreateUserDto dto)
        {
            var (isValid, message) = PasswordValidator.Validate(dto.Password);
            if (!isValid)
                throw new Exception(message);

            var emailExists = await _db.Users
                                       .AnyAsync(u => u.UserEmail == dto.UserEmail);
            if (emailExists)
                throw new Exception("Email is already in use.");

            if (dto.CenterId == 0)
                dto.CenterId = null;

            if (dto.UserType == "CDW")
            {
                if (dto.CenterId is null)
                    throw new Exception("CDW workers must be assigned to a center.");

                var centerExists = await _db.Centers
                                            .AnyAsync(c => c.CenterId == dto.CenterId);
                if (!centerExists)
                    throw new Exception($"Center with ID {dto.CenterId} not found.");
            }

            if (dto.UserType != "CDW")
                dto.CenterId = null;

            var user = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                UserType = dto.UserType,
                CenterId = dto.CenterId,
                UserHashPassword = AuthenticationService.HashPassword(dto.Password),
                UserIsActive = true,
                UserEnrolledAt = DateTime.UtcNow
            };

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
