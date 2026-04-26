using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Reflection.Metadata;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;
using static System.Net.Mime.MediaTypeNames;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace WorkeaseAPI.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db) => _db = db;

        // ── GENERATE ─────────────────────────────────────────────────
        public async Task<ReportSummaryDto> GenerateMonthlyAsync(int cdwUserId,
                                                                  GenerateReportRequest request)
        {
            // Get CDW user and their center
            var cdwUser = await _db.Users
                                   .Include(u => u.Center)
                                   .FirstOrDefaultAsync(u => u.UserId == cdwUserId);

            if (cdwUser?.CenterId is null)
                throw new Exception("CDW user has no assigned center.");

            var centerId = cdwUser.CenterId.Value;
            var centerName = cdwUser.Center!.CenterName;

            // ── Gather data for the month ─────────────────────────────

            // All active children in this center
            var children = await _db.Children
                                    .Where(c => c.CenterId == centerId && c.ChildIsActive)
                                    .ToListAsync();

            var childIds = children.Select(c => c.ChildId).ToList();

            // Health records for this month
            var healthRecords = await _db.HealthRecords
                                         .Where(h => childIds.Contains(h.ChildId)
                                                  && h.HealthRecordDate.Month == request.ReportMonth
                                                  && h.HealthRecordDate.Year == request.ReportMonth)
                                         .ToListAsync();

            // Fee records for this month
            var feeRecords = await _db.FeeRecords
                                      .Where(f => childIds.Contains(f.ChildId)
                                               && f.FeeRecordMonth == request.ReportMonth
                                               && f.FeeRecordMonth == request.ReportMonth)
                                      .ToListAsync();

            // ── Build statistics ──────────────────────────────────────
            var stats = BuildStats(children.Count, healthRecords, feeRecords);

            // ── Generate file ─────────────────────────────────────────
            var fileData = request.ReportMonth.ToString().ToUpper() == "WORD"
                ? GenerateWordReport(centerName, request, stats)
                : GeneratePdfReport(centerName, request, stats);

            // ── Save report record to DB ──────────────────────────────
            var report = new Report
            {
                ReportId = cdwUserId,
                CenterId = centerId,
                ReportMonth = request.ReportMonth,
                ReportYear = request.ReportYear,
                ReportFormat = request.ReportFormat,
                Observations = request.Observations,
                ReportFileData = fileData,
                ReportGeneratedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return new ReportSummaryDto
            {
                ReportId = report.ReportId,
                CenterName = centerName,
                ReportMonth = report.ReportMonth,
                ReportYear = report.ReportYear,
                ReportFormat = report.ReportFormat,
                ReportGeneratedAt = report.ReportGeneratedAt
            };
        }

        // ── DOWNLOAD ──────────────────────────────────────────────────
        public async Task<(byte[] file, string format)> DownloadAsync(int reportId, int cdwUserId)
        {
            var report = await _db.Reports
                                  .FirstOrDefaultAsync(r => r.ReportId == reportId
                                                         && r.UserId == cdwUserId);

            if (report?.ReportFileData is null)
                throw new Exception("Report not found.");

            return (report.ReportFileData, report.ReportFormat);
        }

        // ── GET MY REPORTS LIST ───────────────────────────────────────
        public async Task<IEnumerable<ReportSummaryDto>> GetMyReportsAsync(int cdwUserId)
        {
            return await _db.Reports
                            .Include(r => r.Center)
                            .Where(r => r.UserId == cdwUserId)
                            .OrderByDescending(r => r.ReportYear)
                            .ThenByDescending(r => r.ReportMonth)
                            .Select(r => new ReportSummaryDto
                            {
                                ReportId = r.ReportId,
                                CenterName = r.Center!.CenterName,
                                ReportMonth = r.ReportMonth,
                                ReportYear = r.ReportYear,
                                ReportFormat = r.ReportFormat,
                                ReportGeneratedAt = r.ReportGeneratedAt
                            })
                            .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // STATS BUILDER — computes everything from raw records
        // ─────────────────────────────────────────────────────────────

        private ReportStats BuildStats(int totalChildren,
                                       List<HealthRecord> health,
                                       List<FeeRecord> fees)
        {
            var presentCount = health.Count(h => h.HealthRecordIsPresent);
            var absentCount = totalChildren - presentCount;
            var recordedCount = health.Count;

            // BMI stats
            var bmis = health.Where(h => h.HealthRecordHeightCm > 0)
                                        .Select(h => h.HealthRecordWeigtKg /
                                                   ((h.HealthRecordHeightCm / 100) * (h.HealthRecordHeightCm / 100)))
                                        .ToList();
            var averageBmi = bmis.Any() ? Math.Round(bmis.Average(), 2) : 0;
            var underweightCount = bmis.Count(b => b < 18.5m);
            var normalCount = bmis.Count(b => b >= 18.5m && b < 25m);
            var overweightCount = bmis.Count(b => b >= 25m);

            // Fee stats
            var paidCount = fees.Count(f => f.FeeRecordIsPaid);
            var unpaidCount = fees.Count(f => !f.FeeRecordIsPaid);
            var totalCollected = fees.Where(f => f.FeeRecordIsPaid).Sum(f => f.FeeRecordMonthlyAmount);
            var totalOutstanding = fees.Where(f => !f.FeeRecordIsPaid).Sum(f => f.FeeRecordMonthlyAmount);

            return new ReportStats
            {
                TotalChildren = totalChildren,
                RecordedCount = recordedCount,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                AverageBmi = averageBmi,
                UnderweightCount = underweightCount,
                NormalCount = normalCount,
                OverweightCount = overweightCount,
                PaidCount = paidCount,
                UnpaidCount = unpaidCount,
                TotalCollected = totalCollected,
                TotalOutstanding = totalOutstanding
            };
        }

        // ─────────────────────────────────────────────────────────────
        // WORD REPORT GENERATOR (OpenXML / DocX library)
        // Install: dotnet add package DocumentFormat.OpenXml
        // ─────────────────────────────────────────────────────────────

        private byte[] GenerateWordReport(string centerName,
                                          GenerateReportRequest request,
                                          ReportStats s)
        {
            var monthName = new DateTime(request.ReportYear, request.ReportMonth, 1)
                                .ToString("MMMM yyyy");

            using var stream = new MemoryStream();
            using var doc = WordprocessingDocument.Create(
                                   stream,
                                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document);

            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(

                // ── Title ─────────────────────────────────────────────
                MakeParagraph("WorkEase Monthly Report", "36", bold: true, center: true),
                MakeParagraph(centerName, "28", bold: true, center: true),
                MakeParagraph(monthName, "24", bold: false, center: true),
                MakeParagraph("", "24"),  // spacer

                // ── Overview ──────────────────────────────────────────
                MakeParagraph("Overview", "28", bold: true),
                MakeParagraph(
                    $"This month, {centerName} monitored a total of {s.TotalChildren} children. " +
                    $"Of these, {s.PresentCount} were present during the health monitoring session, " +
                    $"while {s.AbsentCount} were absent. " +
                    $"Health records were successfully recorded for {s.RecordedCount} children.",
                    "24"),
                MakeParagraph("", "24"),

                // ── Health Summary ────────────────────────────────────
                MakeParagraph("Health Summary", "28", bold: true),
                MakeParagraph(
                    $"The average Body Mass Index (BMI) recorded this month was {s.AverageBmi}, " +
                    $"which reflects the general nutritional status of the children in the center. " +
                    $"Among those recorded, {s.NormalCount} children fall within the normal BMI range, " +
                    $"{s.UnderweightCount} are classified as underweight, and " +
                    $"{s.OverweightCount} are classified as overweight or obese. " +
                    $"Children with abnormal BMI readings have been flagged for follow-up.",
                    "24"),
                MakeParagraph("", "24"),

                // ── Fee Collection ────────────────────────────────────
                MakeParagraph("Miscellaneous Fee Collection", "28", bold: true),
                MakeParagraph(
                    $"Out of {s.TotalChildren} families, {s.PaidCount} have fully settled their " +
                    $"miscellaneous fees for {new DateTime(request.ReportYear, request.ReportMonth, 1):MMMM yyyy}, " +
                    $"with a total collected amount of ₱{s.TotalCollected:N2}. " +
                    $"{s.UnpaidCount} families remain unpaid, representing an outstanding " +
                    $"balance of ₱{s.TotalOutstanding:N2}. Reminders have been issued to affected families.",
                    "24"),
                MakeParagraph("", "24"),

                // ── CDW Observations ──────────────────────────────────
                MakeParagraph("Notable Observations", "28", bold: true),
                MakeParagraph(
                    string.IsNullOrWhiteSpace(request.Observations)
                        ? "No additional observations recorded for this month."
                        : request.Observations,
                    "24"),
                MakeParagraph("", "24"),

                // ── Footer ────────────────────────────────────────────
                MakeParagraph($"Report generated on {DateTime.Now:MMMM dd, yyyy}",
                              "20", center: true),
                MakeParagraph("WorkEase — Child Development Worker System",
                              "20", center: true)
            ));

            doc.Save();
            return stream.ToArray();
        }

        // Helper — builds a styled paragraph for Word
        private static Paragraph MakeParagraph(string text,
                                                string fontSize,
                                                bool bold = false,
                                                bool center = false)
        {
            var run = new Run(new Text(text));
            var rpr = new RunProperties();

            rpr.AppendChild(new FontSize { Val = fontSize });
            if (bold) rpr.AppendChild(new Bold());
            run.PrependChild(rpr);

            var para = new Paragraph(run);
            if (center)
            {
                para.PrependChild(new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }));
            }

            return para;
        }

        // ─────────────────────────────────────────────────────────────
        // PDF REPORT GENERATOR (QuestPDF)
        // Install: dotnet add package QuestPDF
        // ─────────────────────────────────────────────────────────────

        private byte[] GeneratePdfReport(string centerName,
                                         GenerateReportRequest request,
                                         ReportStats s)
        {
            var monthName = new DateTime(request.ReportYear, request.ReportMonth, 1)
                                .ToString("MMMM yyyy");

            var observations = string.IsNullOrWhiteSpace(request.Observations)
                ? "No additional observations recorded for this month."
                : request.Observations;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // ── Title ──────────────────────────────────────
                        col.Item().AlignCenter().Text("WorkEase Monthly Report")
                            .FontSize(22).Bold();
                        col.Item().AlignCenter().Text(centerName)
                            .FontSize(16).Bold();
                        col.Item().AlignCenter().Text(monthName)
                            .FontSize(13);
                        col.Item().LineHorizontal(1);

                        // ── Overview ───────────────────────────────────
                        col.Item().Text("Overview").FontSize(14).Bold();
                        col.Item().Text(
                            $"This month, {centerName} monitored a total of {s.TotalChildren} children. " +
                            $"Of these, {s.PresentCount} were present during the health monitoring session, " +
                            $"while {s.AbsentCount} were absent. " +
                            $"Health records were successfully recorded for {s.RecordedCount} children.");

                        // ── Health Summary ─────────────────────────────
                        col.Item().Text("Health Summary").FontSize(14).Bold();
                        col.Item().Text(
                            $"The average Body Mass Index (BMI) recorded this month was {s.AverageBmi}, " +
                            $"reflecting the general nutritional status of the children in the center. " +
                            $"Among those recorded, {s.NormalCount} children fall within the normal BMI range, " +
                            $"{s.UnderweightCount} are classified as underweight, and " +
                            $"{s.OverweightCount} are classified as overweight or obese. " +
                            $"Children with abnormal BMI readings have been flagged for follow-up.");

                        // ── Fee Collection ─────────────────────────────
                        col.Item().Text("Miscellaneous Fee Collection").FontSize(14).Bold();
                        col.Item().Text(
                            $"Out of {s.TotalChildren} families, {s.PaidCount} have fully settled " +
                            $"their miscellaneous fees for {monthName}, " +
                            $"with a total collected amount of ₱{s.TotalCollected:N2}. " +
                            $"{s.UnpaidCount} families remain unpaid, representing an outstanding " +
                            $"balance of ₱{s.TotalOutstanding:N2}. Reminders have been issued.");

                        // ── Observations ───────────────────────────────
                        col.Item().Text("Notable Observations").FontSize(14).Bold();
                        col.Item().Text(observations);

                        col.Item().LineHorizontal(1);

                        // ── Footer ─────────────────────────────────────
                        col.Item().AlignCenter()
                            .Text($"Report generated on {DateTime.Now:MMMM dd, yyyy}")
                            .FontSize(9).Italic();
                        col.Item().AlignCenter()
                            .Text("WorkEase — Child Development Worker System")
                            .FontSize(9).Italic();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
