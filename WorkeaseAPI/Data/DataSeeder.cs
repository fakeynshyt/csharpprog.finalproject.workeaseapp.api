using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WorkeaseAPI.Data;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Models;

namespace WorkEaseAPI.Data
{
    public static class DataSeeder
    {
        private const decimal MONTHLY_CONTRIBUTION = 100.00m;

        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedCentersAsync(db);
            await SeedUsersAsync(db);
            await SeedChildrenAsync(db);
            await SeedHealthRecordsAsync(db);
            await SeedFeeRecordsAsync(db);
        }

        // ── 1. CDW CENTERS ────────────────────────────────────────
        private static async Task SeedCentersAsync(AppDbContext db)
        {
            if (await db.Centers.AnyAsync()) return;

            db.Centers.AddRange(
                new Center { CenterName = "CDW Poblacion", CenterLocation = "Poblacion, Burgos, Pangasinan" },
                new Center { CenterName = "CDW Cabayugan", CenterLocation = "San Pascual, Burgos, Pangasinan" }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Centers seeded.");
        }

        // ── 2. USERS ──────────────────────────────────────────────
        private static async Task SeedUsersAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync()) return;

            var center1 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Poblacion");
            var center2 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Cabayugan");

            db.Users.AddRange(

                new User
                {
                    UserName = "Renier Rafols",
                    UserEmail = "admin@workease.com",
                    UserHashPassword = HashPassword("Admin@123"),
                    UserType = "Admin",
                    CenterId = null,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Maria Santos",
                    UserEmail = "maria@workease.com",
                    UserHashPassword = HashPassword("Cdw@12345"),
                    UserType = "CDW",
                    CenterId = center1.CenterId,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Jose Reyes",
                    UserEmail = "jose@workease.com",
                    UserHashPassword = HashPassword("Cdw@12345"),
                    UserType = "CDW",
                    CenterId = center2.CenterId,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Ana Dela Cruz",
                    UserEmail = "ana@gmail.com",
                    UserHashPassword = HashPassword("Parent@12345"),
                    UserType = "Parent",
                    CenterId = null,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Pedro Bautista",
                    UserEmail = "pedro@gmail.com",
                    UserHashPassword = HashPassword("Parent@12345"),
                    UserType = "Parent",
                    CenterId = null,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Rosa Mendoza",
                    UserEmail = "rosa@gmail.com",
                    UserHashPassword = HashPassword("Parent@12345"),
                    UserType = "Parent",
                    CenterId = null,
                    UserIsActive = true,
                    UserEnrolledAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Users seeded.");
        }

        // ── 3. CHILDREN ───────────────────────────────────────────
        private static async Task SeedChildrenAsync(AppDbContext db)
        {
            if (await db.Children.AnyAsync()) return;

            var center1 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Poblacion");
            var center2 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Cabayugan");
            var ana = await db.Users.FirstAsync(u => u.UserEmail == "ana@gmail.com");
            var pedro = await db.Users.FirstAsync(u => u.UserEmail == "pedro@gmail.com");
            var rosa = await db.Users.FirstAsync(u => u.UserEmail == "rosa@gmail.com");

            db.Children.AddRange(

                new Child
                {
                    ChildFirstName = "Liam",
                    ChildLastName = "Dela Cruz",
                    ChildBirthDate = new DateTime(2019, 3, 15),
                    ChildGender = "Male",
                    CenterId = center1.CenterId,
                    GuardianId = ana.UserId,
                    ChildIsActive = true,
                    ChildEnrolledDate = new DateTime(2025, 1, 1), // enrolled January 2025
                    ChildUpdatedDate = DateTime.UtcNow
                },
                new Child
                {
                    ChildFirstName = "Sofia",
                    ChildLastName = "Bautista",
                    ChildBirthDate = new DateTime(2020, 7, 22),
                    ChildGender = "Female",
                    CenterId = center1.CenterId,
                    GuardianId = pedro.UserId,
                    ChildIsActive = true,
                    ChildEnrolledDate = new DateTime(2025, 1, 1),
                    ChildUpdatedDate = DateTime.UtcNow
                },
                new Child
                {
                    ChildFirstName = "Marco",
                    ChildLastName = "Mendoza",
                    ChildBirthDate = new DateTime(2018, 11, 8),
                    ChildGender = "Male",
                    CenterId = center1.CenterId,
                    GuardianId = rosa.UserId,
                    ChildIsActive = true,
                    ChildEnrolledDate = new DateTime(2025, 1, 1),
                    ChildUpdatedDate = DateTime.UtcNow
                },
                new Child
                {
                    ChildFirstName = "Isabella",
                    ChildLastName = "Garcia",
                    ChildBirthDate = new DateTime(2021, 1, 30),
                    ChildGender = "Female",
                    CenterId = center2.CenterId,
                    GuardianId = null,
                    ChildIsActive = true,
                    ChildEnrolledDate = new DateTime(2025, 1, 1),
                    ChildUpdatedDate = DateTime.UtcNow
                },
                new Child
                {
                    ChildFirstName = "Carlos",
                    ChildLastName = "Torres",
                    ChildBirthDate = new DateTime(2019, 6, 12),
                    ChildGender = "Male",
                    CenterId = center2.CenterId,
                    GuardianId = null,
                    ChildIsActive = true,
                    ChildEnrolledDate = new DateTime(2025, 1, 1),
                    ChildUpdatedDate = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Children seeded.");
        }

        // ── 4. HEALTH RECORDS ─────────────────────────────────────
        private static async Task SeedHealthRecordsAsync(AppDbContext db)
        {
            if (await db.HealthRecords.AnyAsync()) return;

            var liam = await db.Children.FirstAsync(c => c.ChildFirstName == "Liam");
            var sofia = await db.Children.FirstAsync(c => c.ChildFirstName == "Sofia");
            var marco = await db.Children.FirstAsync(c => c.ChildFirstName == "Marco");
            var isabella = await db.Children.FirstAsync(c => c.ChildFirstName == "Isabella");
            var carlos = await db.Children.FirstAsync(c => c.ChildFirstName == "Carlos");
            var maria = await db.Users.FirstAsync(u => u.UserEmail == "maria@workease.com");
            var jose = await db.Users.FirstAsync(u => u.UserEmail == "jose@workease.com");

            db.HealthRecords.AddRange(

                // Liam
                new HealthRecord
                {
                    ChildId = liam.ChildId,
                    HealthRecordDate = new DateTime(2025, 3, 10),
                    HealthRecordWeigtKg = 14.5m,
                    HealthRecordHeightCm = 98.0m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Healthy. Good appetite.",
                    HealthRecordedByUserId = maria.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },
                new HealthRecord
                {
                    ChildId = liam.ChildId,
                    HealthRecordDate = new DateTime(2025, 4, 10),
                    HealthRecordWeigtKg = 14.8m,
                    HealthRecordHeightCm = 98.5m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Weight gaining steadily.",
                    HealthRecordedByUserId = maria.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },

                // Sofia
                new HealthRecord
                {
                    ChildId = sofia.ChildId,
                    HealthRecordDate = new DateTime(2025, 3, 10),
                    HealthRecordWeigtKg = 11.2m,
                    HealthRecordHeightCm = 85.0m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Slightly underweight. Advised parent.",
                    HealthRecordedByUserId = maria.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },
                new HealthRecord
                {
                    ChildId = sofia.ChildId,
                    HealthRecordDate = new DateTime(2025, 4, 10),
                    HealthRecordWeigtKg = 11.5m,
                    HealthRecordHeightCm = 85.5m,
                    HealthRecordIsPresent = false,
                    HealthRecordNotes = "Absent — sick.",
                    HealthRecordedByUserId = maria.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },

                // Marco
                new HealthRecord
                {
                    ChildId = marco.ChildId,
                    HealthRecordDate = new DateTime(2025, 4, 10),
                    HealthRecordWeigtKg = 18.0m,
                    HealthRecordHeightCm = 110.0m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Normal. Very active.",
                    HealthRecordedByUserId = maria.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },

                // Isabella
                new HealthRecord
                {
                    ChildId = isabella.ChildId,
                    HealthRecordDate = new DateTime(2025, 4, 11),
                    HealthRecordWeigtKg = 9.8m,
                    HealthRecordHeightCm = 78.0m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Normal for age.",
                    HealthRecordedByUserId = jose.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                },

                // Carlos
                new HealthRecord
                {
                    ChildId = carlos.ChildId,
                    HealthRecordDate = new DateTime(2025, 4, 11),
                    HealthRecordWeigtKg = 16.0m,
                    HealthRecordHeightCm = 105.0m,
                    HealthRecordIsPresent = true,
                    HealthRecordNotes = "Healthy.",
                    HealthRecordedByUserId = jose.UserId,
                    HealthRecordIsSync = true,
                    HealthRecordCreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Health records seeded.");
        }

        // ── 5. FEE RECORDS ────────────────────────────────────────
        private static async Task SeedFeeRecordsAsync(AppDbContext db)
        {
            if (await db.FeeRecords.AnyAsync()) return;

            var liam = await db.Children.FirstAsync(c => c.ChildFirstName == "Liam");
            var sofia = await db.Children.FirstAsync(c => c.ChildFirstName == "Sofia");
            var marco = await db.Children.FirstAsync(c => c.ChildFirstName == "Marco");
            var isabella = await db.Children.FirstAsync(c => c.ChildFirstName == "Isabella");
            var carlos = await db.Children.FirstAsync(c => c.ChildFirstName == "Carlos");
            var maria = await db.Users.FirstAsync(u => u.UserEmail == "maria@workease.com");
            var jose = await db.Users.FirstAsync(u => u.UserEmail == "jose@workease.com");

            db.FeeRecords.AddRange(

                // ── LIAM ─────────────────────────────────────────
                // Jan — paid (no carryover, first month)
                new FeeRecord
                {
                    ChildId = liam.ChildId,
                    FeeRecordMonth = 1,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 1, 15),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(1, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },
                // Feb — paid (no carryover)
                new FeeRecord
                {
                    ChildId = liam.ChildId,
                    FeeRecordMonth = 2,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 2, 10),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(2, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },
                // Mar — paid (no carryover)
                new FeeRecord
                {
                    ChildId = liam.ChildId,
                    FeeRecordMonth = 3,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 3, 12),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(3, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },
                // Apr — paid (no carryover)
                new FeeRecord
                {
                    ChildId = liam.ChildId,
                    FeeRecordMonth = 4,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 4, 10),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(4, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },

                // ── SOFIA ────────────────────────────────────────
                // Jan — paid
                new FeeRecord
                {
                    ChildId = sofia.ChildId,
                    FeeRecordMonth = 1,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 1, 20),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(1, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },
                // Feb — unpaid → overdue
                new FeeRecord
                {
                    ChildId = sofia.ChildId,
                    FeeRecordMonth = 2,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(2, 2025),
                    FeeRecordIsOverdue = true,  // past due
                    FeeRecordedByUserId = maria.UserId
                },
                // Mar — unpaid + 100 carryover from Feb → overdue
                new FeeRecord
                {
                    ChildId = sofia.ChildId,
                    FeeRecordMonth = 3,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 100.00m,  // Feb unpaid
                    FeeRecordTotalAmount = 200.00m,  // 100 + 100
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 3, 15),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(3, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = maria.UserId
                },
                // Apr — paid (no carryover since Mar was paid)
                new FeeRecord
                {
                    ChildId = sofia.ChildId,
                    FeeRecordMonth = 4,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(4, 2025),
                    FeeRecordIsOverdue = true,  // past due
                    FeeRecordedByUserId = maria.UserId
                },

                // ── MARCO ────────────────────────────────────────
                // Jan — unpaid → overdue
                new FeeRecord
                {
                    ChildId = marco.ChildId,
                    FeeRecordMonth = 1,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(1, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = maria.UserId
                },
                // Feb — unpaid + 100 carryover → overdue
                new FeeRecord
                {
                    ChildId = marco.ChildId,
                    FeeRecordMonth = 2,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 100.00m,  // Jan unpaid
                    FeeRecordTotalAmount = 200.00m,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(2, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = maria.UserId
                },
                // Mar — unpaid + 200 carryover → overdue
                new FeeRecord
                {
                    ChildId = marco.ChildId,
                    FeeRecordMonth = 3,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 200.00m,  // Feb total unpaid
                    FeeRecordTotalAmount = 300.00m,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(3, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = maria.UserId
                },
                // Apr — unpaid + 300 carryover → overdue
                new FeeRecord
                {
                    ChildId = marco.ChildId,
                    FeeRecordMonth = 4,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 300.00m,  // Mar total unpaid
                    FeeRecordTotalAmount = 400.00m,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(4, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = maria.UserId
                },

                // ── ISABELLA ─────────────────────────────────────
                // Jan — paid
                new FeeRecord
                {
                    ChildId = isabella.ChildId,
                    FeeRecordMonth = 1,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 1, 25),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(1, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },
                // Feb — paid
                new FeeRecord
                {
                    ChildId = isabella.ChildId,
                    FeeRecordMonth = 2,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 2, 20),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(2, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },
                // Mar — paid
                new FeeRecord
                {
                    ChildId = isabella.ChildId,
                    FeeRecordMonth = 3,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 3, 18),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(3, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },
                // Apr — paid
                new FeeRecord
                {
                    ChildId = isabella.ChildId,
                    FeeRecordMonth = 4,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 4, 11),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(4, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },

                // ── CARLOS ───────────────────────────────────────
                // Jan — unpaid → overdue
                new FeeRecord
                {
                    ChildId = carlos.ChildId,
                    FeeRecordMonth = 1,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(1, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = jose.UserId
                },
                // Feb — paid (cleared Jan carryover)
                new FeeRecord
                {
                    ChildId = carlos.ChildId,
                    FeeRecordMonth = 2,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 100.00m,  // Jan unpaid
                    FeeRecordTotalAmount = 200.00m,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 2, 25),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(2, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },
                // Mar — paid
                new FeeRecord
                {
                    ChildId = carlos.ChildId,
                    FeeRecordMonth = 3,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = true,
                    FeeRecordPaidDate = new DateTime(2025, 3, 22),
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(3, 2025),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = jose.UserId
                },
                // Apr — unpaid → overdue
                new FeeRecord
                {
                    ChildId = carlos.ChildId,
                    FeeRecordMonth = 4,
                    FeeRecordYear = 2025,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = 0.00m,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(4, 2025),
                    FeeRecordIsOverdue = true,
                    FeeRecordedByUserId = jose.UserId
                }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Fee records seeded.");
        }

        // ── PASSWORD HASHER ───────────────────────────────────────
        private static string HashPassword(string plain)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
            return Convert.ToHexString(bytes);
        }
    }
}