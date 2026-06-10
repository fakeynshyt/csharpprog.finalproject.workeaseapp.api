using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkeaseAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Centers",
                columns: table => new
                {
                    CenterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CenterLocation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centers", x => x.CenterId);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    SyncLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncLogUserId = table.Column<int>(type: "int", nullable: false),
                    SyncLoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncLogRecordHealthRecordsSynced = table.Column<int>(type: "int", nullable: false),
                    SyncLogRecordFeeRecordsSynced = table.Column<int>(type: "int", nullable: false),
                    SyncLogFailedSyncedCounts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.SyncLogId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserHashPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CenterId = table.Column<int>(type: "int", nullable: true),
                    UserIsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "CenterId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    ChildId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildBirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChildGender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuardianId = table.Column<int>(type: "int", nullable: true),
                    CenterId = table.Column<int>(type: "int", nullable: false),
                    ChildEnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChildUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChildIsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.ChildId);
                    table.ForeignKey(
                        name: "FK_Children_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "CenterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Children_Users_GuardianId",
                        column: x => x.GuardianId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportFileData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: false),
                    CdwCenterId = table.Column<int>(type: "int", nullable: true),
                    ReportMonth = table.Column<int>(type: "int", nullable: true),
                    ReportYear = table.Column<int>(type: "int", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_Centers_CdwCenterId",
                        column: x => x.CdwCenterId,
                        principalTable: "Centers",
                        principalColumn: "CenterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reports_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    AttendanceRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    AttendanceRecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceRecordIsPresent = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceRecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    AttendanceRecordCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceRecordUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceRecordIsSync = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.AttendanceRecordId);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_AttendanceRecordedByUserId",
                        column: x => x.AttendanceRecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeRecords",
                columns: table => new
                {
                    FeeRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeRecordReceiptNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    FeeRecordMonth = table.Column<int>(type: "int", nullable: false),
                    FeeRecordYear = table.Column<int>(type: "int", nullable: false),
                    FeeRecordMonthlyAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FeeRecordCarryOver = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FeeRecordTotalAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FeeRecordIsPaid = table.Column<bool>(type: "bit", nullable: false),
                    FeeRecordPaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeRecordDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeeRecordIsOverdue = table.Column<bool>(type: "bit", nullable: false),
                    FeeRecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    FeeRecordCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeeRecordUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeRecords", x => x.FeeRecordId);
                    table.ForeignKey(
                        name: "FK_FeeRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeeRecords_Users_FeeRecordedByUserId",
                        column: x => x.FeeRecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Growths",
                columns: table => new
                {
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    Reading = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Cognitive = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Motor = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Social = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Creative = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LifeSkills = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SpentPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Growths", x => x.ChildId);
                    table.ForeignKey(
                        name: "FK_Growths_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthRecords",
                columns: table => new
                {
                    HealthRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthRecordWeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HealthRecordHeightCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HealthRecordNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthRecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordIsSync = table.Column<bool>(type: "bit", nullable: false),
                    HealthRecordCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthRecordUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthRecords", x => x.HealthRecordId);
                    table.ForeignKey(
                        name: "FK_HealthRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthRecords_Users_HealthRecordedByUserId",
                        column: x => x.HealthRecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceRecordedByUserId",
                table: "AttendanceRecords",
                column: "AttendanceRecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ChildId_AttendanceRecordDate",
                table: "AttendanceRecords",
                columns: new[] { "ChildId", "AttendanceRecordDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Children_CenterId",
                table: "Children",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_GuardianId",
                table: "Children",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeRecords_ChildId",
                table: "FeeRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeRecords_FeeRecordedByUserId",
                table: "FeeRecords",
                column: "FeeRecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_ChildId",
                table: "HealthRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_HealthRecordedByUserId",
                table: "HealthRecords",
                column: "HealthRecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CdwCenterId",
                table: "Reports",
                column: "CdwCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedByUserId",
                table: "Reports",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CenterId",
                table: "Users",
                column: "CenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "FeeRecords");

            migrationBuilder.DropTable(
                name: "Growths");

            migrationBuilder.DropTable(
                name: "HealthRecords");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Centers");
        }
    }
}
