using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;

namespace WorkeaseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService) =>
            _authService = authService;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);

            if(response is null) return Unauthorized(new { message = "Invalid email or password" });

            return Ok(response);
        }

    }
}
