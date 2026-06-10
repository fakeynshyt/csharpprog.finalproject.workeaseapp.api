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
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
        public DbSet<Growth> Growths => Set<Growth>();

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
                 .WithMany()                     // ✅ WithMany instead of WithOne
                 .HasForeignKey(c => c.GuardianId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);

            // ── HealthRecord → Child ──────────────────────────────
            modelBuilder.Entity<HealthRecord>()
                .HasOne(h => h.Child)
                .WithMany()
                .HasForeignKey(h => h.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HealthRecord>()
                .HasOne(h => h.RecordedByUser)
                .WithMany()
                .HasForeignKey(h => h.HealthRecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BMI is computed — do not store in DB
            modelBuilder.Entity<HealthRecord>()
                .Ignore(h => h.HealthRecordBmi);

            // ── Attendance → Child ────────────────────────────────────
            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.Child)
                .WithMany()
                .HasForeignKey(a => a.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Attendance → RecordedByUser ───────────────────────────
            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.AttendanceRecordedByUser)
                .WithMany()
                .HasForeignKey(a => a.AttendanceRecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.ChildId, a.AttendanceRecordDate })
                .IsUnique(false);

            // ── FeeRecord → Child ─────────────────────────────────
            modelBuilder.Entity<FeeRecord>()
                .HasOne(f => f.Child)
                .WithMany()
                .HasForeignKey(f => f.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeeRecord>()
                .HasOne(f => f.RecordedByUser)
                .WithMany()
                .HasForeignKey(f => f.FeeRecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Report → CdwUser ──────────────────────────────────
            modelBuilder.Entity<Report>()
                .HasOne(r => r.GeneratedByUser)
                .WithMany()
                .HasForeignKey(r => r.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Center)
                .WithMany()
                .HasForeignKey(r => r.CdwCenterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .Property(r => r.ReportFileData)
                .HasColumnType("varbinary(max)");

            modelBuilder.Entity<Growth>(entity =>
            {
                // ✅ ChildId is both PK and FK — one growth record per child
                entity.HasKey(g => g.ChildId);

                entity.HasOne(g => g.Child)
                      .WithOne()
                      .HasForeignKey<Growth>(g => g.ChildId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ Each category capped at 100 — enforce at service level
                entity.Property(g => g.Reading).HasDefaultValue(0);
                entity.Property(g => g.Cognitive).HasDefaultValue(0);
                entity.Property(g => g.Motor).HasDefaultValue(0);
                entity.Property(g => g.Social).HasDefaultValue(0);
                entity.Property(g => g.Creative).HasDefaultValue(0);
                entity.Property(g => g.LifeSkills).HasDefaultValue(0);
                entity.Property(g => g.TotalPoints).HasDefaultValue(0);
                entity.Property(g => g.SpentPoints).HasDefaultValue(0);
            });

            // ── Decimal precision ─────────────────────────────────
            modelBuilder.Entity<FeeRecord>()
    .Property(f => f.FeeRecordMonthlyAmount).HasPrecision(10, 2);
            modelBuilder.Entity<FeeRecord>()
                .Property(f => f.FeeRecordCarryOver).HasPrecision(10, 2);
            modelBuilder.Entity<FeeRecord>()
                .Property(f => f.FeeRecordTotalAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Report>()
                .Property(r => r.ReportFileData).IsRequired(false); 
        }
    }
}
