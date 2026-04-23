using System.Security.Cryptography;
using System.Text;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthenticationService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
        {
            var user = _db.Users
                .FirstOrDefault(u => u.UserEmail == loginRequestDto.LoginEmail && u.UserIsActive);

            if (user == null) return null;

            var hashed = HashPassword(loginRequestDto.LoginPassword);
            
            if(user.UserHashPassword != hashed) return null;

            var token = GenerateToken(user);

            return new LoginResponseDto
            {
                Token = token,
                UserType = user.UserType,
                UserId = user.UserId,
                UserName = user.UserName,
            };
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Email,
                    user.UserEmail),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Name,
                    user.UserName),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role,
                    user.UserType)
            };

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                                        key,
                                        Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                       .WriteToken(token);
        }

        public static string HashPassword(string plainPassword)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword));
            return Convert.ToHexString(bytes);
        }
    }
}
