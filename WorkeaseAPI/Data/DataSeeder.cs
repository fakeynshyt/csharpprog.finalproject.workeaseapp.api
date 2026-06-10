using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WorkeaseAPI.Data;
using WorkeaseAPI.Models;

namespace WorkEaseAPI.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedCentersAsync(db);
            await SeedUsersAsync(db);
        }

        // ── 1. CDW CENTERS ────────────────────────────────────────
        private static async Task SeedCentersAsync(AppDbContext db)
        {
            if (await db.Centers.AnyAsync()) return;

            db.Centers.AddRange(
                new Center { CenterName = "CDW Poblacion", CenterLocation = "Poblacion, Burgos, Pangasinan" },
                new Center { CenterName = "CDW Cabayugan", CenterLocation = "Papallasen, Burgos, Pangasinan" }
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Centers seeded.");
        }

        private static async Task SeedUsersAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync()) return;

            var center1 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Poblacion");
            var center2 = await db.Centers.FirstAsync(c => c.CenterName == "CDW Cabayugan");

            db.Users.AddRange(

                new User
                {
                    UserName = "Renier Rafols",
                    UserEmail = "rafols@workease.burgos.ph",
                    UserHashPassword = HashPassword("Rafols@123"),
                    UserType = "Admin",
                    CenterId = null,
                    UserIsActive = true,
                    UserCreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserName = "Kendrick Radovan",
                    UserEmail = "radovan@workease.burgos.ph",
                    UserHashPassword = HashPassword("Kendrick@123"),
                    UserType = "Admin",
                    CenterId = null,
                    UserIsActive = true,
                    UserCreatedAt = DateTime.UtcNow
                }
                
            );

            await db.SaveChangesAsync();
            Console.WriteLine("✅ Users seeded.");
        }

        // ── PASSWORD HASHER ───────────────────────────────────────
        private static string HashPassword(string plain)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
            return Convert.ToHexString(bytes);
        }
    }
}