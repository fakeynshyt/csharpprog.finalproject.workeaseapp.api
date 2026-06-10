using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Services;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IUserService _userService;

        public AttendanceController(IAttendanceService attendanceService, IUserService userService)
        {
            _attendanceService = attendanceService;
            _userService = userService;
        }
            

        // GET api/attendance?day=5&month=4&year=2025&childId=1&centerId=1
        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int day,
                                                [FromQuery] int month,
                                                [FromQuery] int year,
                                                [FromQuery] int? childId,
                                                [FromQuery] int? centerId)
        {
            try
            {
                // ✅ All three required
                if (day == 0 || month == 0 || year == 0)
                    return BadRequest(new { message = "Day, month and year are required." });

                var role = GetUserType();
                var userId = GetUserId();

                if (role == "CDW" && !centerId.HasValue && !childId.HasValue)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    centerId = user?.CenterId;
                }

                var records = await _attendanceService.GetFilteredAttendanceRecordAsync(
                    day, month, year, childId, centerId);

                return Ok(records);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/attendance/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var record = await _attendanceService.GetAttendanceRecordByIdAsync(id);
                return record is null ? NotFound() : Ok(record);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // POST api/attendance
        [HttpPost]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Create(CreateAttendanceDto dto)
        {
            try
            {
                var created = await _attendanceService.CreateAttendanceRecordAsync(dto, GetUserId());
                return Ok(created);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // PUT api/attendance/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Update(int id, UpdateAttendanceDto dto)
        {
            try
            {
                var result = await _attendanceService.UpdateAttendanceRecordAsync(id, dto);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // DELETE api/attendance/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _attendanceService.DeleteAttendanceRecordAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string GetUserType() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
