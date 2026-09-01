using AracGorevFormu.Models;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; } = null!;
        public DbSet<GorevFormu> GorevFormlari { get; set; } = null!;
        public DbSet<AdminUser> AdminUsers { get; set; } = null!;
        public DbSet<SmtpAyari> SmtpAyarlari { get; set; } = null!;
        public DbSet<ArventoAyari> ArventoAyarlari { get; set; } = null!;
        public DbSet<AracBakim> AracBakimlari { get; set; } = null!;
        public DbSet<HgsGecis> HgsGecisleri { get; set; } = null!;
        public DbSet<SystemLog> SystemLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>().ToTable("Vehicles");
            modelBuilder.Entity<Vehicle>().HasIndex(v => v.Plaka);

            modelBuilder.Entity<GorevFormu>().ToTable("GorevFormlari");
            modelBuilder.Entity<GorevFormu>().HasIndex(f => f.TakipKodu);

            modelBuilder.Entity<AdminUser>().ToTable("AdminUsers");
            modelBuilder.Entity<SmtpAyari>().ToTable("SmtpAyarlari");
            modelBuilder.Entity<ArventoAyari>().ToTable("ArventoAyarlari");
            modelBuilder.Entity<AracBakim>().ToTable("AracBakimlari");
            modelBuilder.Entity<HgsGecis>().ToTable("HgsGecisleri");
            modelBuilder.Entity<SystemLog>().ToTable("SystemLogs");
        }
    }
}
