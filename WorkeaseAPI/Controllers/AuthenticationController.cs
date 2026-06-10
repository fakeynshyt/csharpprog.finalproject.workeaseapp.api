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
        // Step 1 — declare the service
        private readonly IAuthenticationService _authService;

        // Step 2 — inject through constructor
        public AuthenticationController(IAuthenticationService authService)
            => _authService = authService;

        // Step 3 — use it in endpoints
        [HttpPost("login")]
        public async Task<IActionResult> Login(DTOs.LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                if (response is null)
                    return Unauthorized(new { message = "Invalid email or password." });
                return Ok(response);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }
    }
}
