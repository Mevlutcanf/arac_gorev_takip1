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
        public DbSet<Makine> Makineler { get; set; } = null!;
        public DbSet<MakineBakim> MakineBakimlari { get; set; } = null!;
        public DbSet<DosyaEki> DosyaEkleri { get; set; } = null!;
        public DbSet<MailTaslak> MailTaslaklari { get; set; } = null!;
        public DbSet<Kategori> Kategoriler { get; set; } = null!;

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

            modelBuilder.Entity<AracBakim>().Property(b => b.Maliyet).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<HgsGecis>().Property(h => h.Tutar).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MakineBakim>().Property(m => m.Maliyet).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Makine>().ToTable("Makineler");
            modelBuilder.Entity<MakineBakim>().ToTable("MakineBakimlari");
            modelBuilder.Entity<DosyaEki>().ToTable("DosyaEkleri");
            modelBuilder.Entity<MailTaslak>().ToTable("MailTaslaklari");
            modelBuilder.Entity<Kategori>().ToTable("Kategoriler");
        }
    }
}

