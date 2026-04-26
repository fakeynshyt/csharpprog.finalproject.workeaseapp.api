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

        // POST api/reports/generate
        // CDW only — generate monthly report
        [HttpPost("generate")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Generate(GenerateReportRequest request)
        {
            var userId = GetUserId();
            var summary = await _reportService.GenerateMonthlyAsync(userId, request);
            return Ok(summary);
        }

        // GET api/reports/{id}/download
        // CDW only — download their generated report file
        [HttpGet("{id}/download")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetUserId();
            var (file, format) = await _reportService.DownloadAsync(id, userId);

            var (contentType, extension) = format.ToUpper() == "WORD"
                ? ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx")
                : ("application/pdf", "pdf");

            return File(file, contentType, $"WorkEase_Report_{id}.{extension}");
        }

        // GET api/reports/mine
        // CDW — see list of their previously generated reports
        [HttpGet("mine")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetMyReports()
        {
            var userId = GetUserId();
            var reports = await _reportService.GetMyReportsAsync(userId);
            return Ok(reports);
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
