using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db) => _db = db;

        public async Task<ReportListDto> GenerateMasterListAsync(GenerateMasterListDto dto,
                                                          int userId)
        {
            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            var centerName = center.CenterName;
            var centerAddress = center.CenterLocation;

            var children = await _db.Children
                                    .Include(c => c.Center)
                                    .Include(c => c.Guardian)
                                    .Where(c => c.CenterId == dto.CenterId
                                             && c.ChildIsActive == true)
                                    .OrderBy(c => c.ChildLastName)
                                    .ThenBy(c => c.ChildFirstName)
                                    .ToListAsync();

            var childIds = children.Select(c => c.ChildId).ToList();

            var latestHealth = await _db.HealthRecords
                                        .Where(h => childIds.Contains(h.ChildId))
                                        .GroupBy(h => h.ChildId)
                                        .Select(g => g.OrderByDescending(h => h.HealthRecordDate)
                                                      .First())
                                        .ToListAsync();

            var healthDict = latestHealth.ToDictionary(h => h.ChildId);

            var rows = new List<MasterListRowDto>();

            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];
                var h = healthDict.GetValueOrDefault(c.ChildId);
                var ageMonths = AgeHelper.GetAgeInMonths(c.ChildBirthDate);

                rows.Add(new MasterListRowDto
                {
                    RowNumber = i + 1,
                    ChildId = c.ChildId,
                    FullName = c.ChildFirstName + " " + c.ChildLastName,
                    Gender = c.ChildGender,
                    Address = c.ChildAddress,
                    BirthDate = c.ChildBirthDate,
                    AgeInMonths = ageMonths,
                    WeightKg = h?.HealthRecordWeightKg ?? 0,
                    HeightCm = h?.HealthRecordHeightCm ?? 0,
                    LastWeighDate = h?.HealthRecordDate,
                    Guardian = c.Guardian?.UserName ?? "N/A",
                    Notes = h?.HealthRecordNotes ?? string.Empty
                });
            }

            byte[] fileData = GenerateMasterListExcel(
                                  rows, centerName, centerAddress,
                                  dto.CycleInfo, dto.SchoolYear,
                                  dto.PreparedBy, dto.NotedBy);

            string title = $"Master List — {centerName} — {DateTime.Now:MMMM dd, yyyy}";

            var report = new Report
            {
                ReportTitle = title,
                ReportType = "MasterList",
                ReportFormat = "Excel",
                ReportFileData = fileData,
                GeneratedByUserId = userId,
                CdwCenterId = dto.CenterId,
                GeneratedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return await BuildReportListDto(report);
        }

        private byte[] GenerateMasterListExcel(List<MasterListRowDto> rows,
                                        string centerName,
                                        string centerAddress,
                                        string cycleInfo,
                                        string schoolYear,
                                        string preparedBy,
                                        string notedBy)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Master List");

            int totalCols = 11;

            ws.Cell(1, 4).Value = "MASTERLIST OF CHILDREN";
            ws.Range(1, 4, 1, 8).Merge();
            ws.Cell(1, 4).Style.Font.Bold = true;
            ws.Cell(1, 4).Style.Font.FontSize = 14;
            ws.Cell(1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // ── ROW 2: Cycle Info (Centered) ─────────────────────────────────────
            ws.Cell(2, 4).Value = $"BURGOS — {cycleInfo}";
            ws.Range(2, 4, 2, 8).Merge();
            ws.Cell(2, 4).Style.Font.Bold = true;
            ws.Cell(2, 4).Style.Font.FontSize = 12;
            ws.Cell(2, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // ── ROW 3: School Year (Centered) ────────────────────────────────────
            var cell34 = ws.Cell(3, 4);
            ws.Range(3, 4, 3, 8).Merge();

            // 1. Initialize RichText
            var rt = cell34.CreateRichText();

            // 2. Add "CY " and make it bold
            var cyRun = rt.AddText("CY ");
            cyRun.Bold = true;

            // 3. Add the school year, make it bold AND underline it
            var schoolYearRun = rt.AddText(schoolYear);
            schoolYearRun.Bold = true;
            schoolYearRun.Underline = XLFontUnderlineValues.Single;

            // 4. Set overall cell alignment and size
            cell34.Style.Font.FontSize = 11;
            cell34.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(4, 1).Value = $"{centerName.ToUpper()} CHILD DEVELOPMENT CENTER";
            ws.Range(4, 1, 4, totalCols).Merge();
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 11;
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell(5, 1).Value = centerAddress;
            ws.Range(5, 1, 5, totalCols).Merge();
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 1).Style.Font.FontSize = 11;
            ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell(6, 1).Value = "DISTRICT 1";
            ws.Range(6, 1, 6, totalCols).Merge();
            ws.Cell(6, 1).Style.Font.Bold = true;
            ws.Cell(6, 1).Style.Font.FontSize = 10;
            ws.Cell(6, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            int headerRow = 8;
            string[] headers =
            {
        "No.", "Address", "Name of\nParents/Guardian",
        "Full Name of Child", "Gender", "Date of\nBirth",
        "Actual Date\nWeighing", "Weight\n(kg)",
        "Height\n(cm)", "Age in\nMonths", "Summary\nNotes"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(headerRow).Height = 35;

            // ── DATA ROWS ─────────────────────────────────────────────────────────
            int dataRow = headerRow + 1;
            foreach (var row in rows)
            {
                ws.Cell(dataRow, 1).Value = row.RowNumber;
                ws.Cell(dataRow, 2).Value = row.Address;
                ws.Cell(dataRow, 3).Value = row.Guardian;
                ws.Cell(dataRow, 4).Value = row.FullName;
                ws.Cell(dataRow, 5).Value = row.Gender;
                ws.Cell(dataRow, 6).Value = row.BirthDate.ToString("MM/dd/yyyy");
                ws.Cell(dataRow, 7).Value = row.LastWeighDate?.ToString("MM/dd/yyyy") ?? string.Empty;
                ws.Cell(dataRow, 8).Value = row.WeightKg > 0 ? row.WeightKg.ToString("F2") : string.Empty;
                ws.Cell(dataRow, 9).Value = row.HeightCm > 0 ? row.HeightCm.ToString("F2") : string.Empty;
                ws.Cell(dataRow, 10).Value = row.AgeInMonths;
                ws.Cell(dataRow, 11).Value = row.Notes;

                for (int col = 1; col <= totalCols; col++)
                {
                    var cell = ws.Cell(dataRow, col);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Alignment.Horizontal = (col == 3 || col == 4 || col == 11)
                        ? XLAlignmentHorizontalValues.Left
                        : XLAlignmentHorizontalValues.Center;
                }
                ws.Row(dataRow).Height = 18;
                dataRow++;
            }

            // Fill empty rows for consistent appearance
            int extraRows = Math.Max(0, 20 - rows.Count);
            for (int i = 0; i < extraRows; i++)
            {
                for (int col = 1; col <= totalCols; col++)
                {
                    ws.Cell(dataRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                ws.Row(dataRow).Height = 18;
                dataRow++;
            }

            // ── LEGEND & SIGNATURES ──────────────────────────────────────────────
            dataRow += 2;
            int sigRow = dataRow;

            // Legend
            ws.Cell(sigRow, 1).Value = "Nutritional Status Legend";
            ws.Cell(sigRow, 1).Style.Font.Bold = true;
            ws.Cell(sigRow + 1, 1).Value = "N – Normal";
            ws.Cell(sigRow + 2, 1).Value = "UW – Underweight";
            ws.Cell(sigRow + 3, 1).Value = "OW – Overweight";

            // Prepared By
            ws.Cell(sigRow, 4).Value = "Prepared By:";
            ws.Cell(sigRow, 4).Style.Font.Bold = true;
            ws.Cell(sigRow + 3, 4).Value = preparedBy;
            ws.Cell(sigRow + 3, 4).Style.Font.Bold = true;
            ws.Cell(sigRow + 3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(sigRow + 3, 4, sigRow + 3, 5).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Noted By
            ws.Cell(sigRow, 7).Value = "Noted By:";
            ws.Cell(sigRow, 7).Style.Font.Bold = true;
            ws.Cell(sigRow + 3, 7).Value = notedBy;
            ws.Cell(sigRow + 3, 7).Style.Font.Bold = true;
            ws.Cell(sigRow + 3, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(sigRow + 3, 7, sigRow + 3, 8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // ── COLUMN WIDTHS ─────────────────────────────────────────────────────
            ws.Column(1).Width = 5;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 20;
            ws.Column(4).Width = 22;
            ws.Column(5).Width = 8;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 14;
            ws.Column(8).Width = 10;
            ws.Column(9).Width = 10;
            ws.Column(10).Width = 10;
            ws.Column(11).Width = 20;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<ReportListDto> BuildReportListDto(Report r)
        {
            var user = r.GeneratedByUser ?? await _db.Users.FindAsync(r.GeneratedByUserId);
            var center = r.Center ?? (r.CdwCenterId.HasValue
                ? await _db.Centers.FindAsync(r.CdwCenterId.Value)
                : null);

            return new ReportListDto
            {
                ReportId = r.ReportId,
                ReportTitle = r.ReportTitle,
                ReportType = r.ReportType,
                ReportFormat = r.ReportFormat,
                GeneratedBy = user?.UserName ?? string.Empty,
                CenterName = center?.CenterName,
                ReportMonth = r.ReportMonth,
                ReportYear = r.ReportYear,
                GeneratedAt = r.GeneratedAt
            };
        }

        // Services/ReportService.cs

        public async Task<ReportListDto> GeneratePdfSummaryAsync(GeneratePdfSummaryDto dto,
                                                                  int userId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            var centerName = center.CenterName;
            var centerAddress = center.CenterLocation;

            // Get active children
            var children = await _db.Children
                                    .Include(c => c.Guardian)
                                    .Where(c => c.CenterId == dto.CenterId
                                             && c.ChildIsActive == true)
                                    .OrderBy(c => c.ChildLastName)
                                    .ToListAsync();

            var childIds = children.Select(c => c.ChildId).ToList();

            // Get latest health per child
            var latestHealth = await _db.HealthRecords
                                        .Where(h => childIds.Contains(h.ChildId))
                                        .GroupBy(h => h.ChildId)
                                        .Select(g => g.OrderByDescending(h => h.HealthRecordDate)
                                                      .First())
                                        .ToListAsync();

            var healthDict = latestHealth.ToDictionary(h => h.ChildId);

            // Build rows
            var rows = new List<MasterListRowDto>();
            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];
                var h = healthDict.GetValueOrDefault(c.ChildId);
                var ageMonths = AgeHelper.GetAgeInMonths(c.ChildBirthDate);

                rows.Add(new MasterListRowDto
                {
                    RowNumber = i + 1,
                    ChildId = c.ChildId,
                    FullName = c.ChildFirstName + " " + c.ChildLastName,
                    Gender = c.ChildGender,
                    Address = c.ChildAddress,
                    BirthDate = c.ChildBirthDate,
                    AgeInMonths = ageMonths,
                    WeightKg = h?.HealthRecordWeightKg ?? 0,
                    HeightCm = h?.HealthRecordHeightCm ?? 0,
                    LastWeighDate = h?.HealthRecordDate,
                    Guardian = c.Guardian?.UserName ?? "N/A",
                    Notes = h?.HealthRecordNotes ?? string.Empty
                });
            }

            // Stats
            int totalNormal = rows.Count(r => GetBmiStatus(r.WeightKg, r.HeightCm) == "Normal");
            int totalUnderweight = rows.Count(r => GetBmiStatus(r.WeightKg, r.HeightCm) == "Underweight");
            int totalOverweight = rows.Count(r => GetBmiStatus(r.WeightKg, r.HeightCm) == "Overweight");
            int totalNoData = rows.Count(r => GetBmiStatus(r.WeightKg, r.HeightCm) == "No Data");

            byte[] fileData = GeneratePdfSummary(rows, centerName, centerAddress,
                                                  dto.CycleInfo, dto.SchoolYear,
                                                  totalNormal, totalUnderweight,
                                                  totalOverweight, totalNoData);

            string title = $"PDF Summary — {centerName} — {DateTime.Now:MMMM dd, yyyy}";

            var report = new Report
            {
                ReportTitle = title,
                ReportType = "PDFSummary",
                ReportFormat = "PDF",
                ReportFileData = fileData,
                GeneratedByUserId = userId,
                CdwCenterId = dto.CenterId,
                GeneratedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return await BuildReportListDto(report);
        }

        private string GetBmiStatus(decimal weight, decimal height)
        {
            if (height <= 0 || weight <= 0) return "No Data";
            var heightM = height / 100;
            var bmi = weight / (heightM * heightM);
            return bmi switch
            {
                < 18.5m => "Underweight",
                < 25.0m => "Normal",
                _ => "Overweight"
            };
        }

        private byte[] GeneratePdfSummary(List<MasterListRowDto> rows,
                                   string centerName,
                                   string centerAddress,
                                   string cycleInfo,
                                   string schoolYear,
                                   int totalNormal,
                                   int totalUnderweight,
                                   int totalOverweight,
                                   int totalNoData)
        {
            // 1. MUST set the license before generating (Community is free for individuals/small teams)
            QuestPDF.Settings.License = LicenseType.Community;

            // 2. Use the full namespace QuestPDF.Fluent.Document to avoid the OpenXml conflict
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);

                        // ── Header ─────────────────────────────────────────
                        col.Item().AlignCenter()
                           .Text("MASTERLIST OF CHILDREN")
                           .FontSize(16).Bold();

                        col.Item().AlignCenter()
                           .Text($"BURGOS — {cycleInfo}")
                           .FontSize(13).Bold();

                        col.Item().AlignCenter()
                           .Text($"CY {schoolYear}")
                           .FontSize(11).Bold().Underline();

                        col.Item()
                           .Text($"{centerName.ToUpper()} CHILD DEVELOPMENT CENTER")
                           .FontSize(11).Bold();

                        col.Item()
                           .Text(centerAddress).FontSize(10).Bold();

                        col.Item()
                           .Text("DISTRICT 1").FontSize(10).Bold();

                        col.Item().LineHorizontal(1);

                        // ── Summary Stats ─────────────────────────────────
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Total Children").Bold().FontSize(10);
                                c.Item().AlignCenter().Text(rows.Count.ToString()).FontSize(18).Bold();
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Normal").Bold().FontSize(10).FontColor("#27AE60");
                                c.Item().AlignCenter().Text(totalNormal.ToString()).FontSize(18).Bold().FontColor("#27AE60");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Underweight").Bold().FontSize(10).FontColor("#E67E22");
                                c.Item().AlignCenter().Text(totalUnderweight.ToString()).FontSize(18).Bold().FontColor("#E67E22");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Overweight").Bold().FontSize(10).FontColor("#E74C3C");
                                c.Item().AlignCenter().Text(totalOverweight.ToString()).FontSize(18).Bold().FontColor("#E74C3C");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text("No Data").Bold().FontSize(10).FontColor("#95A5A6");
                                c.Item().AlignCenter().Text(totalNoData.ToString()).FontSize(18).Bold().FontColor("#95A5A6");
                            });
                        });

                        col.Item().PaddingTop(8).Text("Children Details").FontSize(12).Bold();

                        // ── Table ──────────────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(25);  // No.
                                cols.RelativeColumn(3);   // Full Name
                                cols.ConstantColumn(45);  // Gender
                                cols.ConstantColumn(65);  // Birthdate
                                cols.ConstantColumn(45);  // Age Months
                                cols.ConstantColumn(75);  // Date of Weighing
                                cols.ConstantColumn(50);  // Weight
                                cols.ConstantColumn(50);  // Height
                                cols.ConstantColumn(70);  // BMI Status
                                cols.RelativeColumn(2);   // Notes
                            });

                            table.Header(h =>
                            {
                                string[] headers = { "No.", "Full Name", "Gender", "Birthdate", "Age\n(Mo)", "Date of\nWeight", "Wt\n(kg)", "Ht\n(cm)", "Status", "Notes" };
                                foreach (var header in headers)
                                {
                                    h.Cell().Background("#2C3E50").Padding(4).AlignCenter()
                                     .Text(header).FontColor("#FFFFFF").Bold().FontSize(8);
                                }
                            });

                            bool alt = false;
                            foreach (var row in rows)
                            {
                                var bg = alt ? "#F5F5F5" : "#FFFFFF";

                                // Internal Helper for cell styling
                                IContainer CellStyle(IContainer c) => c.Background(bg).Padding(3).AlignCenter();
                                IContainer LeftCellStyle(IContainer c) => c.Background(bg).Padding(3);

                                table.Cell().Element(CellStyle).Text(row.RowNumber.ToString());
                                table.Cell().Element(LeftCellStyle).Text(row.FullName);
                                table.Cell().Element(CellStyle).Text(row.Gender);
                                table.Cell().Element(CellStyle).Text(row.BirthDate.ToString("MM/dd/yyyy"));
                                table.Cell().Element(CellStyle).Text(row.AgeInMonths.ToString());
                                table.Cell().Element(CellStyle).Text(row.LastWeighDate?.ToString("MM/dd/yyyy") ?? "No Record");
                                table.Cell().Element(CellStyle).Text(row.WeightKg > 0 ? $"{row.WeightKg:F2}" : "—");
                                table.Cell().Element(CellStyle).Text(row.HeightCm > 0 ? $"{row.HeightCm:F2}" : "—");

                                // BMI Status logic
                                var bmiStatus = GetBmiStatus(row.WeightKg, row.HeightCm);
                                var bmiColor = bmiStatus == "Normal" ? "#27AE60" : (bmiStatus == "Underweight" ? "#E67E22" : "#E74C3C");

                                table.Cell().Background(bg).Padding(3).AlignCenter()
                                     .Text(bmiStatus).FontColor(bmiColor).Bold();

                                table.Cell().Element(LeftCellStyle).Text(row.Notes).FontSize(8);

                                alt = !alt;
                            }
                        });

                        // ── Footer ─────────────────────────────────────────
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem()
                               .Text("Nutritional Status Legend: N – Normal | UW – Underweight | OW – Overweight")
                               .FontSize(9).Italic();
                        });

                        col.Item().LineHorizontal(1);
                        col.Item().AlignCenter()
                           .Text($"Generated on {DateTime.Now:MMMM dd, yyyy}")
                           .FontSize(8).Italic();
                    });
                });
            }).GeneratePdf();
        }

        public async Task<ReportListDto> GenerateReportFeeAsync(GenerateReportFeeDto dto,
                                                         int userId)
        {
            var monthName = new DateTime(dto.Year, dto.Month, 1).ToString("MMMM yyyy");

            // Get all centers with their CDW workers and fee totals
            var centers = await _db.Centers.ToListAsync();

            var feeData = new List<ReportFeeRowDto>();

            foreach (var center in centers)
            {
                // Get CDW workers for this center
                var cdwNames = await _db.Users
                                        .Where(u => u.CenterId == center.CenterId
                                                 && u.UserType == "CDW"
                                                 && u.UserIsActive == true)
                                        .Select(u => u.UserName)
                                        .ToListAsync();

                // Get children in this center
                var childIds = await _db.Children
                                        .Where(c => c.CenterId == center.CenterId
                                                 && c.ChildIsActive == true)
                                        .Select(c => c.ChildId)
                                        .ToListAsync();

                // Get fee records for this month
                var fees = await _db.FeeRecords
                                    .Where(f => childIds.Contains(f.ChildId)
                                             && f.FeeRecordMonth == dto.Month
                                             && f.FeeRecordYear == dto.Year)
                                    .ToListAsync();

                var totalAmount = fees.Sum(f => f.FeeRecordTotalAmount);
                var totalPaid = fees.Where(f => f.FeeRecordIsPaid)
                                      .Sum(f => f.FeeRecordTotalAmount);

                feeData.Add(new ReportFeeRowDto
                {
                    CenterId = center.CenterId,
                    CenterName = center.CenterName,
                    CenterLocation = center.CenterLocation,
                    AssignedCDW = cdwNames.Any()
                                        ? string.Join(", ", cdwNames)
                                        : "No CDW Assigned",
                    TotalAmount = totalAmount,
                    TotalPaid = totalPaid,
                    TotalUnpaid = totalAmount - totalPaid
                });
            }

            byte[] fileData = GenerateReportFeeExcel(
                                  feeData, monthName,
                                  dto.CycleInfo, dto.SchoolYear,
                                  dto.PreparedBy, dto.NotedBy);

            string title = $"Fee Report — All Centers — {monthName}";

            var report = new Report
            {
                ReportTitle = title,
                ReportType = "ReportFee",
                ReportFormat = "Excel",
                ReportFileData = fileData,
                GeneratedByUserId = userId,
                CdwCenterId = null, // all centers
                ReportMonth = dto.Month,
                ReportYear = dto.Year,
                GeneratedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return await BuildReportListDto(report);
        }

        // Add ReportFeeRowDto
        public class ReportFeeRowDto
        {
            public int CenterId { get; set; }
            public string CenterName { get; set; } = string.Empty;
            public string CenterLocation { get; set; } = string.Empty;
            public string AssignedCDW { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal TotalUnpaid { get; set; }
        }

        private byte[] GenerateReportFeeExcel(List<ReportFeeRowDto> data,
                                               string monthName,
                                               string cycleInfo,
                                               string schoolYear,
                                               string preparedBy,
                                               string notedBy)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fee Report");

            int totalCols = 6;

            // ── Header — same style as MasterList ────────────────────────
            ws.Cell(1, 4).Value = "FEE COLLECTION REPORT";
            ws.Range(1, 4, 1, 8).Merge();
            ws.Cell(1, 4).Style.Font.Bold = true;
            ws.Cell(1, 4).Style.Font.FontSize = 14;
            ws.Cell(1, 4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 4).Value = $"BURGOS — {cycleInfo}";
            ws.Range(2, 4, 2, 8).Merge();
            ws.Cell(2, 4).Style.Font.Bold = true;
            ws.Cell(2, 4).Style.Font.FontSize = 12;
            ws.Cell(2, 4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // ✅ Same CY header style as MasterList
            var cell34 = ws.Cell(3, 4);
            ws.Range(3, 4, 3, 8).Merge();
            var rt = cell34.CreateRichText();
            var cyRun = rt.AddText("CY ");
            cyRun.Bold = true;
            var syRun = rt.AddText(schoolYear);
            syRun.Bold = true;
            syRun.Underline = XLFontUnderlineValues.Single;
            cell34.Style.Font.FontSize = 11;
            cell34.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell(4, 1).Value = "ALL CHILD DEVELOPMENT CENTERS — BURGOS";
            ws.Range(4, 1, 4, totalCols).Merge();
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 11;
            ws.Cell(4, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            ws.Cell(5, 1).Value = $"Month: {monthName}";
            ws.Range(5, 1, 5, totalCols).Merge();
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 1).Style.Font.FontSize = 11;
            ws.Cell(5, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            ws.Cell(6, 1).Value = "DISTRICT 1";
            ws.Range(6, 1, 6, totalCols).Merge();
            ws.Cell(6, 1).Style.Font.Bold = true;
            ws.Cell(6, 1).Style.Font.FontSize = 10;
            ws.Cell(6, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            // ── Column Headers ────────────────────────────────────────────
            int headerRow = 8;
            string[] headers =
            {
        "No.", "Center Name", "Center Location",
        "Assigned CDW", "Total Accumulated Fee", "Total Paid"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            ws.Row(headerRow).Height = 30;

            // ── Data Rows ─────────────────────────────────────────────────
            int dataRow = headerRow + 1;
            bool alt = false;

            foreach (var row in data)
            {
                var bg = alt ? XLColor.FromHtml("#F0F0F0") : XLColor.White;

                ws.Cell(dataRow, 1).Value = data.IndexOf(row) + 1;
                ws.Cell(dataRow, 2).Value = row.CenterName;
                ws.Cell(dataRow, 3).Value = row.CenterLocation;
                ws.Cell(dataRow, 4).Value = row.AssignedCDW;
                ws.Cell(dataRow, 5).Value = row.TotalAmount;
                ws.Cell(dataRow, 6).Value = row.TotalPaid;

                ws.Cell(dataRow, 5).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(dataRow, 6).Style.NumberFormat.Format = "₱#,##0.00";

                for (int col = 1; col <= totalCols; col++)
                {
                    var cell = ws.Cell(dataRow, col);
                    cell.Style.Fill.BackgroundColor = bg;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Alignment.Horizontal =
                        col == 2 || col == 3 || col == 4
                            ? XLAlignmentHorizontalValues.Left
                            : XLAlignmentHorizontalValues.Center;
                }

                ws.Row(dataRow).Height = 20;
                alt = !alt;
                dataRow++;
            }

            // ── Footer — Overall Totals ───────────────────────────────────
            dataRow++;

            ws.Cell(dataRow, 1).Value = "OVERALL TOTAL";
            ws.Range(dataRow, 1, dataRow, 4).Merge();
            ws.Cell(dataRow, 1).Style.Font.Bold = true;
            ws.Cell(dataRow, 1).Style.Font.FontSize = 11;
            ws.Cell(dataRow, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;
            ws.Cell(dataRow, 1).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#2C3E50");
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.White;

            decimal overallTotal = data.Sum(r => r.TotalAmount);
            decimal overallTotalPaid = data.Sum(r => r.TotalPaid);

            ws.Cell(dataRow, 5).Value = overallTotal;
            ws.Cell(dataRow, 6).Value = overallTotalPaid;
            ws.Cell(dataRow, 5).Style.NumberFormat.Format = "₱#,##0.00";
            ws.Cell(dataRow, 6).Style.NumberFormat.Format = "₱#,##0.00";
            ws.Cell(dataRow, 5).Style.Font.Bold = true;
            ws.Cell(dataRow, 6).Style.Font.Bold = true;
            ws.Cell(dataRow, 5).Style.Font.FontSize = 11;
            ws.Cell(dataRow, 6).Style.Font.FontSize = 11;
            ws.Cell(dataRow, 5).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#27AE60");
            ws.Cell(dataRow, 6).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#27AE60");
            ws.Cell(dataRow, 5).Style.Font.FontColor = XLColor.White;
            ws.Cell(dataRow, 6).Style.Font.FontColor = XLColor.White;

            for (int col = 1; col <= totalCols; col++)
            {
                ws.Cell(dataRow, col).Style.Border.OutsideBorder =
                    XLBorderStyleValues.Medium;
            }

            ws.Row(dataRow).Height = 25;

            // ── Total Amount to Pay (Unpaid) ──────────────────────────────
            dataRow++;
            decimal overallUnpaid = data.Sum(r => r.TotalUnpaid);

            ws.Cell(dataRow, 1).Value = "TOTAL AMOUNT TO PAY (ALL CENTERS)";
            ws.Range(dataRow, 1, dataRow, 4).Merge();
            ws.Cell(dataRow, 1).Style.Font.Bold = true;
            ws.Cell(dataRow, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;
            ws.Cell(dataRow, 1).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#E74C3C");
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.White;

            ws.Cell(dataRow, 5).Value = overallUnpaid;
            ws.Range(dataRow, 5, dataRow, 6).Merge();
            ws.Cell(dataRow, 5).Style.NumberFormat.Format = "₱#,##0.00";
            ws.Cell(dataRow, 5).Style.Font.Bold = true;
            ws.Cell(dataRow, 5).Style.Font.FontSize = 11;
            ws.Cell(dataRow, 5).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#E74C3C");
            ws.Cell(dataRow, 5).Style.Font.FontColor = XLColor.White;
            ws.Cell(dataRow, 5).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Row(dataRow).Height = 25;

            // ── Signatures ────────────────────────────────────────────────
            dataRow += 3;

            ws.Cell(dataRow, 2).Value = "Prepared By:";
            ws.Cell(dataRow, 2).Style.Font.Bold = true;

            ws.Cell(dataRow + 3, 2).Value = preparedBy;
            ws.Cell(dataRow + 3, 2).Style.Font.Bold = true;
            ws.Cell(dataRow + 3, 2).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            ws.Range(dataRow + 3, 2, dataRow + 3, 3).Merge();
            ws.Range(dataRow + 3, 2, dataRow + 3, 3)
              .Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            ws.Cell(dataRow, 5).Value = "Noted By:";
            ws.Cell(dataRow, 5).Style.Font.Bold = true;

            ws.Cell(dataRow + 3, 5).Value = notedBy;
            ws.Cell(dataRow + 3, 5).Style.Font.Bold = true;
            ws.Cell(dataRow + 3, 5).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            ws.Range(dataRow + 3, 5, dataRow + 3, 6).Merge();
            ws.Range(dataRow + 3, 5, dataRow + 3, 6)
              .Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // ── Column Widths ─────────────────────────────────────────────
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 30;
            ws.Column(4).Width = 25;
            ws.Column(5).Width = 22;
            ws.Column(6).Width = 18;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<ReportListDto> GenerateNarrativeAsync(GenerateNarrativeDto dto, int userId)
        {
            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            var centerName = center.CenterName;
            var centerAddress = center.CenterLocation;
            var monthName = new DateTime(dto.Year, dto.Month, 1).ToString("MMMM yyyy");

            var children = await _db.Children
                                    .Where(c => c.CenterId == dto.CenterId && c.ChildIsActive == true)
                                    .ToListAsync();

            var childIds = children.Select(c => c.ChildId).ToList();

            var healthRecords = await _db.HealthRecords
                                         .Where(h => childIds.Contains(h.ChildId)
                                                  && h.HealthRecordDate.Month == dto.Month
                                                  && h.HealthRecordDate.Year == dto.Year)
                                         .ToListAsync();

            var attendance = await _db.AttendanceRecords
                                      .Where(a => childIds.Contains(a.ChildId)
                                               && a.AttendanceRecordDate.Month == dto.Month
                                               && a.AttendanceRecordDate.Year == dto.Year)
                                      .ToListAsync();

            var fees = await _db.FeeRecords
                                .Where(f => childIds.Contains(f.ChildId)
                                         && f.FeeRecordMonth == dto.Month
                                         && f.FeeRecordYear == dto.Year)
                                .ToListAsync();

            // Calculate stats - Fixed the "!a.Is" typo here
            int totalChildren = children.Count;
            int totalPresent = attendance.Count(a => a.AttendanceRecordIsPresent);
            int totalAbsent = attendance.Count(a => !a.AttendanceRecordIsPresent);
            int totalWeighed = healthRecords.Count;

            var bmis = healthRecords
                       .Where(h => h.HealthRecordHeightCm > 0)
                       .Select(h =>
                       {
                           var hm = h.HealthRecordHeightCm / 100;
                           return (decimal)(h.HealthRecordWeightKg / (hm * hm));
                       }).ToList();

            int normalCount = bmis.Count(b => b >= 18.5m && b < 25m);
            int underweightCount = bmis.Count(b => b < 18.5m);
            int overweightCount = bmis.Count(b => b >= 25m);
            decimal avgBmi = bmis.Any() ? Math.Round(bmis.Average(), 2) : 0;

            int paidCount = fees.Count(f => f.FeeRecordIsPaid);
            int unpaidCount = fees.Count(f => !f.FeeRecordIsPaid);
            decimal totalCollected = fees.Where(f => f.FeeRecordIsPaid).Sum(f => f.FeeRecordTotalAmount);
            decimal totalOutstanding = fees.Where(f => !f.FeeRecordIsPaid).Sum(f => f.FeeRecordTotalAmount);

            byte[] fileData = GenerateNarrativeWord(
                centerName, centerAddress, monthName,
                dto.CycleInfo, dto.SchoolYear,
                dto.PreparedBy, dto.NotedBy,
                dto.Observations,
                totalChildren, totalPresent, totalAbsent,
                totalWeighed, normalCount, underweightCount,
                overweightCount, avgBmi,
                paidCount, unpaidCount,
                totalCollected, totalOutstanding);

            var report = new Report
            {
                ReportTitle = $"Narrative Review — {centerName} — {monthName}",
                ReportType = "Narrative",
                ReportFormat = "Word",
                ReportFileData = fileData,
                GeneratedByUserId = userId,
                CdwCenterId = dto.CenterId,
                ReportMonth = dto.Month,
                ReportYear = dto.Year,
                GeneratedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return await BuildReportListDto(report);
        }

        private byte[] GenerateNarrativeWord(
            string centerName, string centerAddress,
            string monthName, string cycleInfo, string schoolYear,
            string preparedBy, string notedBy, string observations,
            int totalChildren, int totalPresent, int totalAbsent,
            int totalWeighed, int normalCount, int underweightCount,
            int overweightCount, decimal avgBmi,
            int paidCount, int unpaidCount,
            decimal totalCollected, decimal totalOutstanding)
        {
            using var stream = new MemoryStream();
            using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new Body());

            // ── Header ────────────────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "NARRATIVE REPORT", "32", bold: true, center: true));
            body.AppendChild(MakeWordParagraph(
                $"BURGOS — {cycleInfo}", "26", bold: true, center: true));
            body.AppendChild(MakeWordParagraph(
                $"CY {schoolYear}", "24", bold: true, center: true));
            body.AppendChild(MakeWordParagraph(
                $"{centerName.ToUpper()} CHILD DEVELOPMENT CENTER",
                "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                centerAddress, "22", bold: true));
            body.AppendChild(MakeWordParagraph(
                "DISTRICT 1", "22", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"For the Month of: {monthName}", "22", bold: true));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── I. Overview ───────────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "I. OVERVIEW", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"For the month of {monthName}, the {centerName} Child Development " +
                $"Center conducted its regular monitoring activities. A total of " +
                $"{totalChildren} children are currently enrolled and active in the center. " +
                $"The center's Child Development Worker (CDW) carried out home visits, " +
                $"health monitoring, and attendance tracking as part of the monthly " +
                $"implementation of the Supplementary Feeding Program.",
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── II. Attendance ────────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "II. ATTENDANCE", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"Out of {totalChildren} enrolled children, {totalPresent} were present " +
                $"during the monitoring session for {monthName}, while {totalAbsent} " +
                $"were absent. " +
                (totalAbsent > 0
                    ? $"Follow-up visits were conducted for the {totalAbsent} " +
                      $"absent children to ensure their welfare and continued participation."
                    : $"All children were present during the session, reflecting the " +
                      $"strong commitment of families to the program."),
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── III. Health and Nutrition ─────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "III. HEALTH AND NUTRITION MONITORING", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"A total of {totalWeighed} children were weighed and measured during " +
                $"the month of {monthName}. The average Body Mass Index (BMI) recorded " +
                $"was {avgBmi}, indicating the overall nutritional status of the children " +
                $"in the center.",
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph(
                $"Among the {totalWeighed} children recorded:",
                "22"));
            body.AppendChild(MakeWordParagraph(
                $"   • {normalCount} children fall within the Normal nutritional status range.",
                "22"));
            body.AppendChild(MakeWordParagraph(
                $"   • {underweightCount} children are classified as Underweight and " +
                $"have been flagged for supplementary feeding and nutritional counseling.",
                "22"));
            body.AppendChild(MakeWordParagraph(
                $"   • {overweightCount} children are classified as Overweight and " +
                $"are being monitored for dietary guidance.",
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── IV. Miscellaneous Fee Collection ──────────────────────────
            body.AppendChild(MakeWordParagraph(
                "IV. MISCELLANEOUS FEE COLLECTION", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"For the month of {monthName}, a total of {paidCount} out of " +
                $"{paidCount + unpaidCount} families have settled their monthly " +
                $"miscellaneous fees. The total amount collected was " +
                $"₱{totalCollected:N2}. " +
                (unpaidCount > 0
                    ? $"A total of {unpaidCount} families have outstanding balances " +
                      $"amounting to ₱{totalOutstanding:N2}. Reminders and follow-ups " +
                      $"have been issued to the concerned families to settle their dues."
                    : $"All families have fully paid their monthly contributions for " +
                      $"this period, demonstrating excellent compliance from the community."),
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── V. Observations ───────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "V. OBSERVATIONS AND RECOMMENDATIONS", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                string.IsNullOrWhiteSpace(observations)
                    ? "No additional observations recorded for this month."
                    : observations,
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── VI. Conclusion ────────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "VI. CONCLUSION", "24", bold: true));
            body.AppendChild(MakeWordParagraph(
                $"The month of {monthName} has been productive for the {centerName} " +
                $"Child Development Center. The CDW and center staff remain committed " +
                $"to the welfare and development of all enrolled children. Continuous " +
                $"monitoring, feeding, and family engagement activities will be " +
                $"sustained in the coming months to ensure the health and well-being " +
                $"of every child in the community.",
                "22"));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph("", "22"));

            // ── Signatures ────────────────────────────────────────────────
            body.AppendChild(MakeWordParagraph(
                "Prepared By:", "22", bold: true));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph(
                preparedBy, "22", bold: true, center: true));
            body.AppendChild(MakeWordParagraph(
                "Child Development Worker", "20", center: true));
            body.AppendChild(MakeWordParagraph("", "22"));

            body.AppendChild(MakeWordParagraph(
                "Noted By:", "22", bold: true));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph(
                notedBy, "22", bold: true, center: true));
            body.AppendChild(MakeWordParagraph(
                "Barangay Official / Supervisor", "20", center: true));

            body.AppendChild(MakeWordParagraph("", "22"));
            body.AppendChild(MakeWordParagraph(
                $"Date Generated: {DateTime.Now:MMMM dd, yyyy}",
                "18", center: true));

            doc.Save();
            return stream.ToArray();
        }

        public async Task<(byte[] file, string format, string title)> DownloadAsync(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report?.ReportFileData is null)
                throw new Exception("Report not found.");

            return (report.ReportFileData, report.ReportFormat, report.ReportTitle);
        }

        private static Paragraph MakeWordParagraph(string text, string fontSize,
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
                para.PrependChild(new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }));
            return para;
        }
    }
}
