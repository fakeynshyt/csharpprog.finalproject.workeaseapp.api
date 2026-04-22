using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkeaseAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
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
                    UserBirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserGender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserContactNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CenterId = table.Column<int>(type: "int", nullable: true),
                    UserIsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserEnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    ChildEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildHashPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChildBirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChildGender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ChildGuardianContactNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CenterId = table.Column<int>(type: "int", nullable: false),
                    CHildEnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHildUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_Children_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FeeRecords",
                columns: table => new
                {
                    FeeRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    FeeRecordMonth = table.Column<int>(type: "int", nullable: false),
                    FeeRecordYear = table.Column<int>(type: "int", nullable: false),
                    FeeRecordAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FeeRecordDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeeRecordIsPaid = table.Column<bool>(type: "bit", nullable: false),
                    FeeRecordPaidDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeeRecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    FeeRecordIsSync = table.Column<bool>(type: "bit", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "HealthRecords",
                columns: table => new
                {
                    HealthRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthRecordWeigtKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HealthRecordHeightCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HealthRecordIsPresent = table.Column<bool>(type: "bit", nullable: false),
                    HealthRecordNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthRecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordIsSync = table.Column<bool>(type: "bit", nullable: false),
                    HealthRecordCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_CenterId",
                table: "Children",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_UserId",
                table: "Children",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FeeRecords_ChildId",
                table: "FeeRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_ChildId",
                table: "HealthRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CenterId",
                table: "Users",
                column: "CenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeeRecords");

            migrationBuilder.DropTable(
                name: "HealthRecords");

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
