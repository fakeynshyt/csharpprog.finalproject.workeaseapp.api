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
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
            => _reportService = reportService;

        // POST api/reports/pdf-summary
        [HttpPost("pdf-summary")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GeneratePdfSummary(GeneratePdfSummaryDto dto)
        {
            try
            {
                var report = await _reportService.GeneratePdfSummaryAsync(dto, GetUserId());
                return Ok(report);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // POST api/reports/fee-report
        [HttpPost("fee-report")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GenerateReportFee(GenerateReportFeeDto dto)
        {
            try
            {
                var report = await _reportService.GenerateReportFeeAsync(dto, GetUserId());
                return Ok(report);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // POST api/reports/narrative
        [HttpPost("narrative")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GenerateNarrative(GenerateNarrativeDto dto)
        {
            try
            {
                var report = await _reportService.GenerateNarrativeAsync(dto, GetUserId());
                return Ok(report);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost("master-list")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GenerateMasterList(GenerateMasterListDto dto)
        {
            try
            {
                var report = await _reportService.GenerateMasterListAsync(dto, GetUserId());
                return Ok(report);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/reports/{id}/download
        [HttpGet("{id}/download")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var (file, format, title) = await _reportService.DownloadAsync(id);

                var (contentType, extension) = format.ToUpper() switch
                {
                    "EXCEL" => ("application/vnd.openxmlformats-officedocument" +
                                ".spreadsheetml.sheet", "xlsx"),
                    "WORD" => ("application/vnd.openxmlformats-officedocument" +
                                ".wordprocessingml.document", "docx"),
                    _ => ("application/pdf", "pdf")
                };

                var safeTitle = title.Replace(" ", "_")
                                     .Replace("—", "-")
                                     .Replace("/", "-");

                return File(file, contentType, $"{safeTitle}.{extension}");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
