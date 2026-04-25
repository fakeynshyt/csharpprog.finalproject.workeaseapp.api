using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
            => _healthService = healthService;

        // GET api/health?childId=1&centerId=1
        // Admin: all records. CDW: filter by their center.
        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int? childId,
                                                [FromQuery] int? centerId)
        {
            var role = GetUserType();  // ✅ "Admin" or "CDW" as string
            var userId = GetUserId();    // ✅ int

            // If CDW and no centerId filter provided, auto-filter by their center
            if (role == "CDW" && !centerId.HasValue)
            {
                var cdwUser = await GetCdwCenterIdAsync(userId);
                centerId = cdwUser;
            }

            var records = await _healthService.GetFilteredHealthRecordsAsync(childId, centerId);
            return Ok(records);
        }

        // GET api/health/myChild
        // Parent only — their child's health records
        [HttpGet("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildHealth()
        {
            var parentUserId = GetUserId();
            var records = await _healthService.GetHealthRecordByGuardianIdAsync(parentUserId);
            return Ok(records);
        }

        // GET api/health/{id}
        // Admin / CDW — get single health record
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetById(int id)
        {
            var record = await _healthService.GetHealthRecordByIdAsync(id);
            return record is null ? NotFound() : Ok(record);
        }

        // POST api/health
        // Admin / CDW — create a health record
        [HttpPost]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Create(HealthRecord record)
        {
            try
            {
                // Stamp who recorded it from JWT — client cannot fake this
                record.HealthRecordedByUserId = GetUserId();

                var created = await _healthService.CreateHealthRecordAsync(record);
                return CreatedAtAction(nameof(GetById),
                    new { id = created.HealthRecordId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/health/{id}
        // Admin only — edit a health record
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, HealthRecord record)
        {
            var result = await _healthService.UpdateHealthRecordAsync(id, record);
            return result ? NoContent() : NotFound();
        }

        // DELETE api/health/{id}
        // Admin only — delete a health record
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _healthService.DeleteHealthRecordAsync(id);
            return result ? NoContent() : NotFound();
        }

        // ── Helpers ───────────────────────────────────────────────────

        // ✅ Gets userId as int from JWT
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ✅ Gets role as string from JWT — never parse as int
        private string GetUserType() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        // Gets the CDW's assigned centerId from the database
        // Used to auto-filter health records for CDW workers
        private async Task<int?> GetCdwCenterIdAsync(int userId)
        {
            // This requires AppDbContext — inject it in constructor
            return null; // replace with actual DB lookup if needed
        }
    }
}
