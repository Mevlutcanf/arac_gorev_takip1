using AracGorevFormu.Models;
using AracGorevFormu.Services;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Data
{
    public static class SeedData
    {
        public static void Uygula(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

            try
            {
                // Eksik kolonları eklemek için (Eski veritabanını bozmadan güncellemek için)
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Vehicles ADD COLUMN GuncelKm INTEGER;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Vehicles ADD COLUMN SonKonumZamani TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Vehicles ADD COLUMN SonAdres TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE GorevFormlari ADD COLUMN CikisKm INTEGER;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE GorevFormlari ADD COLUMN DonusKm INTEGER;"); } catch { }
                
                // Makine Modülü - Makbuz OCR eklemeleri
                try { context.Database.ExecuteSqlRaw("ALTER TABLE MakineBakimlari ADD COLUMN MakbuzDosyaYolu TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE MakineBakimlari ADD COLUMN MakbuzMetni TEXT;"); } catch { }

                // Tabloları oluştur (Database.EnsureCreated)
                context.Database.EnsureCreated();

                // Eksik kolon/şema testi (yeni eklenen Lokasyon, SabitSurucu, SahiplikTuru vb. kolonların varlık kontrolü)
                _ = context.Vehicles.OrderBy(v => v.Id).Select(v => new { v.Lokasyon, v.SahiplikTuru, v.SabitSurucu, v.SasiNo }).FirstOrDefault();
                _ = context.AracBakimlari.OrderBy(a => a.Id).FirstOrDefault();
                _ = context.HgsGecisleri.OrderBy(h => h.Id).FirstOrDefault();
                _ = context.SystemLogs.OrderBy(l => l.Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Eğer veritabanı dosyası eski kolon/tablo şemasına sahipse sıfırdan OLUŞTURMA!
                // DİKKAT: EnsureDeleted işlemi mevcut verileri sileceği için KALDIRILMIŞTIR.
                // Lütfen eksik kolonları veritabanına manuel olarak veya Migration ile ekleyin.
                logger.LogWarning(ex, "Veritabanı şeması uyumsuz. Ancak veri kaybını önlemek için veritabanı SİLİNMEYECEKTİR.");
                // context.Database.EnsureDeleted(); // VERİ KAYBINA SEBEP OLDUĞU İÇİN İPTAL EDİLDİ
                // context.Database.EnsureCreated(); // Tablolar varsa hata vermez, ama kolon eklemez.
            }

            // 1. Yönetici Hesabı Tohumlama
            if (!context.AdminUsers.Any())
            {
                var (hash, salt) = PasswordHasher.Hashle("Admin123!");
                context.AdminUsers.Add(new AdminUser
                {
                    KullaniciAdi = "admin",
                    AdSoyad = "Abdurrahman Tatlıcı",
                    Rol = "Ana Yönetici",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    AnaYonetici = true
                });
                context.SaveChanges();
            }

            // 2. Araç Filosu Tohumlama (Öz Mal / Kiralık / Şehir ve Sürücü Atamaları)
            if (!context.Vehicles.Any())
            {
                context.Vehicles.AddRange(
                    new Vehicle
                    {
                        Id = 1,
                        Plaka = "06 AB 123",
                        Marka = "Ford",
                        Model = "Transit",
                        Renk = "Beyaz",
                        SahiplikTuru = "Şirket Aracı",
                        SabitSurucu = "Ahmet Yılmaz (Saha Lojistik Sorumlusu)",
                        Lokasyon = "Ankara Genel Merkez",
                        Aktif = true,
                        SasiNo = "WF0XXXTTFX1234567",
                        MotorNo = "20DTH987654",
                        TescilTarihi = new DateTime(2022, 5, 12),
                        MuayeneBitisTarihi = DateTime.Now.AddMonths(8),
                        SigortaBitisTarihi = DateTime.Now.AddMonths(5)
                    },
                    new Vehicle
                    {
                        Id = 2,
                        Plaka = "06 CD 456",
                        Marka = "Volkswagen",
                        Model = "Caddy",
                        Renk = "Beyaz",
                        SahiplikTuru = "Kiralık Araç",
                        SabitSurucu = "Mehmet Demir (İstanbul Bölge Satış Müdürü)",
                        Lokasyon = "İstanbul Şube",
                        Aktif = true,
                        SasiNo = "WV1ZZZ2KZCX7654321",
                        MotorNo = "19TDI123456",
                        TescilTarihi = new DateTime(2021, 3, 20),
                        MuayeneBitisTarihi = DateTime.Now.AddMonths(3),
                        SigortaBitisTarihi = DateTime.Now.AddMonths(11)
                    },
                    new Vehicle
                    {
                        Id = 3,
                        Plaka = "06 EF 789",
                        Marka = "Renault",
                        Model = "Clio",
                        Renk = "Gri",
                        SahiplikTuru = "Şirket Aracı",
                        SabitSurucu = null, // Ortak Havuz
                        Lokasyon = "Ankara Genel Merkez",
                        Aktif = true,
                        SasiNo = "VF155R00123456789",
                        MotorNo = "15DCI654321",
                        TescilTarihi = new DateTime(2023, 1, 15),
                        MuayeneBitisTarihi = DateTime.Now.AddMonths(14),
                        SigortaBitisTarihi = DateTime.Now.AddMonths(9)
                    },
                    new Vehicle
                    {
                        Id = 4,
                        Plaka = "06 GH 321",
                        Marka = "Toyota",
                        Model = "Corolla",
                        Renk = "Siyah",
                        SahiplikTuru = "Kiralık Araç",
                        SabitSurucu = "Canan Kaya (İzmir Bölge Temsilcisi)",
                        Lokasyon = "İzmir Şube",
                        Aktif = true,
                        SasiNo = "NMTBZ3BE40R987654",
                        MotorNo = "18VVT112233",
                        TescilTarihi = new DateTime(2022, 9, 10),
                        MuayeneBitisTarihi = DateTime.Now.AddMonths(6),
                        SigortaBitisTarihi = DateTime.Now.AddMonths(4)
                    }
                );
                context.SaveChanges();
            }

            // 3. Araç Bakım Kayıtları Tohumlama
            if (!context.AracBakimlari.Any())
            {
                context.AracBakimlari.AddRange(
                    new AracBakim
                    {
                        Id = 1,
                        VehicleId = 1,
                        Plaka = "06 AB 123",
                        BakimTarihi = DateTime.Now.AddMonths(-3),
                        BakimTuru = "Periyodik Yağ Bakımı",
                        Km = 45000,
                        YapilanIslemler = "Motor yağı, yağ filtresi, hava filtresi ve polen filtresi değiştirildi.",
                        ServisAdi = "Ford Yetkili Servis (Ankara)",
                        Maliyet = 4500,
                        SonrakiBakimKm = 60000,
                        SonrakiBakimTarihi = DateTime.Now.AddMonths(9)
                    },
                    new AracBakim
                    {
                        Id = 2,
                        VehicleId = 2,
                        Plaka = "06 CD 456",
                        BakimTarihi = DateTime.Now.AddMonths(-1),
                        BakimTuru = "Fren & Lastik Değişimi",
                        Km = 62000,
                        YapilanIslemler = "Ön balatalar ve 4 adet Michelin yazlık lastik sıfır takıldı.",
                        ServisAdi = "Oto Pratik Servis",
                        Maliyet = 12800,
                        SonrakiBakimKm = 75000,
                        SonrakiBakimTarihi = DateTime.Now.AddMonths(11)
                    }
                );
                context.SaveChanges();
            }

            // 4. HGS ve Ceza Kayıtları (Sadece Gerçek API ve Veritabanı verileri kullanılır)
            if (!context.HgsGecisleri.Any())
            {
                // Sahte geçiş verisi tamamen kaldırıldı. Sadece gerçek API verileri kullanılacak.
            }

            // 5. SMTP ve Arvento Ayarları Tohumlama
            if (!context.SmtpAyarlari.Any())
            {
                context.SmtpAyarlari.Add(new SmtpAyari { Id = 1, SmtpServer = "smtp.gmail.com", Port = 587, EnableSsl = true, Aktif = false });
                context.SaveChanges();
            }

            if (!context.ArventoAyarlari.Any())
            {
                context.ArventoAyarlari.Add(new ArventoAyari
                {
                    Id = 1,
                    ApiUrl = "https://ws.arvento.com/v1/report.asmx",
                    KullaniciAdi = "2a8c3b606478c8525c5a4dcbd187d76a",
                    Sifre = "30907e1b96aafaeb26b65270d271457b",
                    Aktif = true
                });
                context.SaveChanges();
            }
        }
    }
}
