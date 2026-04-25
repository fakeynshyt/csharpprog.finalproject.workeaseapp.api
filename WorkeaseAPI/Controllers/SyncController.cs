using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;

namespace WorkeaseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;

        public SyncController(ISyncService syncService)
            => _syncService = syncService;

        // GET api/sync/initial-download
        // Called once after first login — downloads everything for this user
        [HttpGet("initial-download")]
        [Authorize(Policy = "AllRoles")]
        public async Task<IActionResult> InitialDownload()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value!;

            var package = await _syncService.GetInitialPackageAsync(userId, role);
            return Ok(package);
        }

        // POST api/sync/upload
        // CDW only — sends all records saved offline
        [HttpPost("upload")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Upload(SyncPayloadDto payload)
        {
            // Override the userId from the JWT — client cannot fake this
            payload.CdwUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _syncService.ProcessSyncAsync(payload);
            return Ok(result);
        }
    }
}
