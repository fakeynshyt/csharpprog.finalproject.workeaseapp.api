using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest loginRequestDto);
    }
}
