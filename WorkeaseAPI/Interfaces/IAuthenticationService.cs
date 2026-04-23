using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
    }
}
