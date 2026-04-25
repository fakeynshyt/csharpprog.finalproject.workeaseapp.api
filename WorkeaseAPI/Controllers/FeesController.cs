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
    public class FeesController : ControllerBase
    {
        private readonly IFeeService _feeService;

        public FeesController(IFeeService feeService)
            => _feeService = feeService;

        // GET api/fees?centerId=1&month=5&year=2025
        // Admin: all records. CDW: filter by their center.
        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll([FromQuery] int? centerId,
                                                [FromQuery] int? month,
                                                [FromQuery] int? year) =>
            Ok(await _feeService.GetFilteredFeeRecordsAsync(centerId, month, year));

        // GET api/fees/myChild
        // Parent only — their child's fee records
        [HttpGet("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildFees()
        {
            var parentUserId = GetUserId();
            var fees = await _feeService.GetFeeRecordsByGuardianId(parentUserId);
            return Ok(fees);
        }

        // POST api/fees
        // Admin only — create a fee record for a child
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(FeeRecord record)
        {
            record.FeeRecordedByUserId = GetUserId();
            var created = await _feeService.CreateFeeRecord(record);
            return Ok(created);
        }

        // PUT api/fees/{id}/pay
        // CDW / Admin — mark a fee as paid
        [HttpPut("{id}/pay")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> MarkPaid(int id) =>
            await _feeService.MarkFeeRecordPaidAsync(id) ? NoContent() : NotFound();

        // PUT api/fees/{id}
        // Admin only — edit fee amount or month/year
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, FeeRecord record) =>
            await _feeService.UpdateFeeRecordAsync(id, record) ? NoContent() : NotFound();

        // DELETE api/fees/{id}
        // Admin only — delete a fee record
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id) =>
            await _feeService.DeleteFeeRecordAsync(id) ? NoContent() : NotFound();

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
