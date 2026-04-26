using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;
using WorkeaseAPI.Services;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController : ControllerBase
    {
        private readonly IFeeService _feeService;
        private readonly IAutoFeeService _autoFeeService;
        private readonly AppDbContext _db;

        public FeesController(IFeeService feeService,
                              IAutoFeeService autoFeeService,
                              AppDbContext db)
        {
            _feeService = feeService;
            _autoFeeService = autoFeeService;
            _db = db;
        }

        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int? centerId,
                                                [FromQuery] int? month,
                                                [FromQuery] int? year)
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

                var fees = await _feeService.GetFilteredFeeRecordAsync(centerId, month, year);
                return Ok(fees);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildFees()
        {
            try
            {
                var fees = await _feeService.GetFeeRecordByGuardianUserIdAsync(GetUserId());
                return Ok(fees);
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
                var fee = await _feeService.GetFeeRecordByIdAsync(id);
                return fee is null ? NotFound() : Ok(fee);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateFeeDto dto)  // ✅ DTO
        {
            try
            {
                var created = await _feeService.CreateFeeRecordAsync(dto, GetUserId());
                return Ok(created);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPut("{id}/pay")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> MarkPaid(int id)
        {
            try
            {
                var result = await _feeService.MarkFeeRecordAsPaidAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, UpdateFeeDto dto)  // ✅ DTO
        {
            try
            {
                var result = await _feeService.UpdateFeeRecordAsync(id, dto);
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
                var result = await _feeService.DeleteFeeRecordAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost("generate-monthly")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GenerateMonthly()
        {
            try
            {
                await _autoFeeService.GenerateMonthlyFeesAsync();
                return Ok(new { message = "Monthly fees generated successfully." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost("process-overdue")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ProcessOverdue()
        {
            try
            {
                await _autoFeeService.ProcessOverdueFeesAsync();
                return Ok(new { message = "Overdue fees processed successfully." });
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
