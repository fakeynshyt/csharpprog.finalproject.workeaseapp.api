using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;

namespace WorkeaseAPI.Controllers
{
    // Controllers/FeesController.cs
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController : ControllerBase
    {
        private readonly IFeeService _feeService;
        private readonly IAutoFeeService _autoFeeService;
        private readonly IUserService _userService;

        public FeesController(IFeeService feeService,
                              IAutoFeeService autoFeeService,
                              IUserService userService)
        {
            _feeService = feeService;
            _autoFeeService = autoFeeService;
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int? childId,
                                                [FromQuery] int? centerId,
                                                [FromQuery] string? receiptNo)
        {
            try
            {
                var role = GetUserType();
                var userId = GetUserId();

                if (role == "CDW" && !centerId.HasValue && !childId.HasValue)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    centerId = user?.CenterId;
                }

                var fees = await _feeService.GetFilteredFeeRecordAsync(childId, centerId, receiptNo);
                return Ok(fees);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("calculated/{childId}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetCalculated(int childId)
        {
            try
            {
                var result = await _feeService.GetCalculatedFeeByChildAsync(childId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/fees/myChildren
        // GET api/fees/myChildren?childId=2&month=4&year=2026
        [HttpGet("myChildren")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildrenFees(
            [FromQuery] int? childId,
            [FromQuery] int? month,
            [FromQuery] int? year)
        {
            try
            {
                var parentUserId = GetUserId();
                var fees = await _feeService
                                         .GetFeeRecordByGuardianUserIdAsync(parentUserId, childId, month, year);
                return Ok(fees);
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
                if (!result) return NotFound();

                // ✅ Return the updated fee so client sees the change
                var updated = await _feeService.GetFeeRecordByIdAsync(id);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // POST api/fees
        // POST api/fees
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateFeeDto dto)
        {
            try
            {
                // ✅ Returns FeeSummaryDto directly
                var created = await _feeService.CreateFeeRecordAsync(dto, GetUserId());
                return Ok(created);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // PUT api/fees/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, UpdateFeeDto dto)
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

        // DELETE api/fees/{id}
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

        // POST api/fees/process-overdue
        [HttpPost("process-overdue")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ProcessOverdue()
        {
            try
            {
                await _autoFeeService.ProcessOverdueFeesAsync();
                return Ok(new { message = "Overdue fees processed." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // Controllers/FeesController.cs

        // GET api/fees/summary
        // GET api/fees/summary?centerId=1&month=4&year=2026
        [HttpGet("summary")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetOverallSummary([FromQuery] int? centerId,
                                                            [FromQuery] int? month,
                                                            [FromQuery] int? year)
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

                var summary = await _feeService.GetOverallFeesSummaryAsync(
                    centerId, month, year);

                return Ok(summary);
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
