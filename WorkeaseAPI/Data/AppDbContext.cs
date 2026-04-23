using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users => Set <User>();
        public DbSet<Child> Children => Set<Child>();
        public DbSet<Center> Centers => Set<Center>();
        public DbSet<HealthRecord> HealthRecords => Set<HealthRecord>();
        public DbSet<FeeRecord> FeeRecords => Set<FeeRecord>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOne(u => u.Center)
                .WithMany()
                .HasForeignKey(u => u.CenterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Child → CdwCenter ─────────────────────────────────
            modelBuilder.Entity<Child>()
                .HasOne(c => c.Center)
                .WithMany()
                .HasForeignKey(c => c.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Child → Parent User (optional) ───────────────────
            modelBuilder.Entity<Child>()
                .HasOne(c => c.Guardian)
                .WithOne()
                .HasForeignKey<Child>(c => c.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── HealthRecord → Child ──────────────────────────────
            modelBuilder.Entity<HealthRecord>()
                .HasOne(h => h.Child)
                .WithMany()
                .HasForeignKey(h => h.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            // BMI is computed — do not store in DB
            modelBuilder.Entity<HealthRecord>()
                .Ignore(h => h.HealthRecordBmi);

            // ── FeeRecord → Child ─────────────────────────────────
            modelBuilder.Entity<FeeRecord>()
                .HasOne(f => f.Child)
                .WithMany()
                .HasForeignKey(f => f.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Report → CdwUser ──────────────────────────────────
            modelBuilder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Report → CdwCenter ────────────────────────────────
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Center)
                .WithMany()
                .HasForeignKey(r => r.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Report file stored as varbinary(max) ──────────────
            modelBuilder.Entity<Report>()
                .Property(r => r.FileData)
                .HasColumnType("varbinary(max)");

            // ── Decimal precision ─────────────────────────────────
            modelBuilder.Entity<HealthRecord>()
                .Property(h => h.HealthRecordWeigtKg).HasPrecision(5, 2);
            modelBuilder.Entity<HealthRecord>()
                .Property(h => h.HealthRecordHeightCm).HasPrecision(5, 2);
            modelBuilder.Entity<FeeRecord>()
                .Property(f => f.FeeRecordAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Report>()
                .Property(r => r.FileData).IsRequired(false); 
        }
    }
}
