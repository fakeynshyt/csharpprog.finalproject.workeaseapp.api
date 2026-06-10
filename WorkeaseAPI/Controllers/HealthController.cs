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
    // Controllers/HealthController.cs
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;
        private readonly IUserService _userService;

        public HealthController(IHealthService healthService, IUserService userService)
        {
            _healthService = healthService;
            _userService = userService;
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

                // CDW auto-filter by their center
                if (role == "CDW" && !centerId.HasValue)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    centerId = user?.CenterId;
                }

                var records = await _healthService
                                    .GetFilteredHealthRecordsAsync(childId, centerId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/health/myChildren
        // GET api/health/myChildren?childId=2&month=4&year=2026
        [HttpGet("myChildren")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildrenHealth(
            [FromQuery] int? childId,
            [FromQuery] int? month,
            [FromQuery] int? year)
        {
            try
            {
                var parentUserId = GetUserId();
                var records = await _healthService
                                         .GetHealthRecordByGuardianIdAsync(parentUserId, childId, month, year);
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
        public async Task<IActionResult> Create(CreateHealthDto dto)
        {
            try
            {
                var created = await _healthService.CreateHealthRecordAsync(dto, GetUserId());
                return Ok(new
                {
                    message = "Health record created successfully.",
                    healthRecordId = created.HealthRecordId,
                    childId = created.ChildId,
                    bmi = created.HealthBmi
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, UpdateHealthDto dto)
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

        [HttpGet("abnormal-bmi")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAbnormalBmi()
        {
            try
            {
                var result = await _healthService.GetAbnormalChildrenBmiAsync();
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
