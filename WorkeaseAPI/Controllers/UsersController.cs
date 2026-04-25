using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService) =>
            _userService = userService;

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllUsers() =>
            Ok(await _userService.GetAllUsersAsync());


        [HttpGet("me")]
        [Authorize(Policy = "AllRoles")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            return user is null ? NotFound() : Ok(user);

        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminUpdate(int id, User user) =>
            await _userService.AdminUpdateUserAsync(id, user) ? NoContent() : NotFound();

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id) =>
            await _userService.DeleteUserAsync(id) ? NoContent() : NotFound();


        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }    
}
