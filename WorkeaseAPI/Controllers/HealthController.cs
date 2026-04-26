using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
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
        private readonly AppDbContext _db;

        public HealthController(IHealthService healthService, AppDbContext db)
        {
            _healthService = healthService;
            _db = db;
        }

        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int? childId,
                                                [FromQuery] int? centerId)
        {
            try
            {
                var role = GetUserType();
                var userId = GetUserId();

                if (role == "CDW" && !centerId.HasValue)
                {
                    var cdwUser = await _db.Users.FindAsync(userId);
                    centerId = cdwUser?.CenterId;
                }

                var records = await _healthService.GetFilteredHealthRecordsAsync(childId, centerId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildHealth()
        {
            try
            {
                var records = await _healthService.GetHealthRecordByParentUserIdAsync(GetUserId());
                return Ok(records);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var record = await _healthService.GetHealthRecordByIdAsync(id);
                return record is null ? NotFound() : Ok(record);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Create(CreateHealthDto dto)  // ✅ DTO
        {
            try
            {
                var created = await _healthService.CreateHealthRecordAsync(dto, GetUserId());
                return CreatedAtAction(nameof(GetById),
                    new { id = created.HealthRecordId }, created);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, UpdateHealthDto dto)  // ✅ DTO
        {
            try
            {
                var result = await _healthService.UpdateHealthRecordAsync(id, dto);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _healthService.DeleteHealthRecordAsync(id);
                return result ? NoContent() : NotFound();
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
