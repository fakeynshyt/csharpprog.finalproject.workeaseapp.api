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

        private async Task<User?> FindUserAsync(int id) =>
        await _db.Users
                 .Include(u => u.Center)
                 .FirstOrDefaultAsync(u => u.UserId == id
                                        && u.UserIsActive == true);
        public async Task<bool> AdminUpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await FindUserAsync(id);
            if (user is null) return false;

            var emailConflict = await _db.Users
                                         .AnyAsync(u => u.UserEmail == dto.UserEmail
                                                     && u.UserId != id);
            if (emailConflict)
                throw new Exception("Email is already used by another account.");

            if (dto.CenterId == 0) dto.CenterId = null;

            if (dto.UserType == "CDW")
            {
                if (dto.CenterId is null)
                    throw new Exception("CDW workers must be assigned to a center.");

                var centerExists = await _db.Centers
                                            .AnyAsync(c => c.CenterId == dto.CenterId);
                if (!centerExists)
                    throw new Exception($"Center with ID {dto.CenterId} not found.");
            }

            if (dto.UserType != "CDW") dto.CenterId = null;

            user.UserName = dto.UserName;
            user.UserEmail = dto.UserEmail;
            user.UserType = dto.UserType;
            user.CenterId = dto.CenterId;
            user.UserHashPassword = AuthenticationService.HashPassword(dto.UserPasswordHashed);
            user.UserUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<User> CreateUserAsync(CreateUserDto dto)
        {
            var (isValid, message) = PasswordValidator.Validate(dto.UserHashPassword);
            if (!isValid) throw new Exception(message);

            var emailExists = await _db.Users
                                       .AnyAsync(u => u.UserEmail == dto.UserEmail);
            if (emailExists)
                throw new Exception("Email is already in use.");

            if (dto.CenterId == 0) dto.CenterId = null;

            if (dto.UserType == "CDW")
            {
                if (dto.CenterId is null)
                    throw new Exception("CDW workers must be assigned to a center.");

                var centerExists = await _db.Centers
                                            .AnyAsync(c => c.CenterId == dto.CenterId);
                if (!centerExists)
                    throw new Exception($"Center with ID {dto.CenterId} not found.");
            }

            if (dto.UserType != "CDW") dto.CenterId = null;

            var user = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                UserType = dto.UserType,
                CenterId = dto.CenterId,
                UserHashPassword = AuthenticationService.HashPassword(dto.UserHashPassword),
                UserIsActive = true,
                UserCreatedAt = DateTime.UtcNow,
                UserUpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await FindUserAsync(id);
            if (user is null) return false;

            user.UserIsActive = false;
            user.UserUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CdwUserDto?>> GetAllUsersAsync() =>
            await _db.Users
                 .Include(u => u.Center)
                 .Where(u => u.UserIsActive)
                 .OrderBy(u => u.UserType)
                 .ThenBy(u => u.UserName)
                 .Select(u => new CdwUserDto
                 {
                     UserId = u.UserId,
                     UserName = u.UserName,
                     UserEmail = u.UserEmail,
                     UserType = u.UserType,
                     CenterName = u.Center != null ? u.Center.CenterName : null,
                     UserIsActive = u.UserIsActive,
                     UserHashPassword = string.Empty,
                     UserCreatedAt = u.UserCreatedAt,
                     UserUpdatedAt = u.UserUpdatedAt
                 })
                 .ToListAsync();



        public async Task<CdwUserDto?> GetUserByIdAsync(int id)
        {
            var user = await FindUserAsync(id);
            if (user is null) return null;

            return new CdwUserDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserEmail = user.UserEmail,
                UserType = user.UserType,
                CenterName = user.Center?.CenterName,
                CenterId = user.CenterId,
                UserIsActive = user.UserIsActive,
                UserCreatedAt = user.UserCreatedAt,
                UserUpdatedAt = user.UserUpdatedAt
            };
        }
    }
}
