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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>().ToTable("Vehicles");
            modelBuilder.Entity<GorevFormu>().ToTable("GorevFormlari");
            modelBuilder.Entity<AdminUser>().ToTable("AdminUsers");
            modelBuilder.Entity<SmtpAyari>().ToTable("SmtpAyarlari");
            modelBuilder.Entity<ArventoAyari>().ToTable("ArventoAyarlari");
            modelBuilder.Entity<AracBakim>().ToTable("AracBakimlari");
            modelBuilder.Entity<HgsGecis>().ToTable("HgsGecisleri");
        }
    }
}
