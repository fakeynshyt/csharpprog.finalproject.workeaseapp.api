using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;

        public SyncController(ISyncService syncService)
            => _syncService = syncService;

        [HttpGet("initial-download")]
        [Authorize(Policy = "AllRoles")]
        public async Task<IActionResult> InitialDownload()
        {
            try
            {
                var userId = GetUserId();
                var role = GetUserType();
                var package = await _syncService.GetInitialPackageAsync(userId, role);
                return Ok(package);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost("upload")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Upload(SyncPayloadDto payload)
        {
            try
            {
                payload.CdwUserId = GetUserId();
                var result = await _syncService.ProcessSyncAsync(payload);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string GetUserType() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
