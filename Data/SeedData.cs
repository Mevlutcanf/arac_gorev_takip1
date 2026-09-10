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
                // Tabloları oluştur (Database.EnsureCreated)
                context.Database.EnsureCreated();

                // Eksik kolon/şema testi (yeni eklenen Lokasyon, SabitSurucu, SahiplikTuru, RuhsatDosyaIcerigi vb. kolonların varlık kontrolü)
                _ = context.Vehicles.OrderBy(v => v.Id).Select(v => new { v.Lokasyon, v.SahiplikTuru, v.SabitSurucu, v.SasiNo, v.RuhsatDosyaIcerigi, v.RuhsatDosyaAdi, v.RuhsatDosyaTipi }).FirstOrDefault();
                _ = context.AracBakimlari.OrderBy(a => a.Id).FirstOrDefault();
                _ = context.HgsGecisleri.OrderBy(h => h.Id).FirstOrDefault();
                _ = context.SystemLogs.OrderBy(l => l.Id).FirstOrDefault();
                _ = context.DosyaEkleri.OrderBy(d => d.Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Eğer veritabanı dosyası eski kolon/tablo şemasına sahipse sıfırdan OLUŞTURMA!
                logger.LogWarning(ex, "Veritabanı şeması uyumsuz. Ancak veri kaybını önlemek için veritabanı SİLİNMEYECEKTİR.");
            }

            try
            {
                // Eksik kolonları (MuayeneBildirimGonderildi ve SigortaBildirimGonderildi) manuel olarak ekle
                context.Database.ExecuteSqlRaw(@"
                    IF COL_LENGTH('Vehicles', 'MuayeneBildirimGonderildi') IS NULL
                    BEGIN
                        ALTER TABLE Vehicles ADD MuayeneBildirimGonderildi bit NOT NULL DEFAULT 0;
                    END
                    
                    IF COL_LENGTH('Vehicles', 'SigortaBildirimGonderildi') IS NULL
                    BEGIN
                        ALTER TABLE Vehicles ADD SigortaBildirimGonderildi bit NOT NULL DEFAULT 0;
                    END
                    
                    IF OBJECT_ID('MailTaslaklari', 'U') IS NULL
                    BEGIN
                        CREATE TABLE MailTaslaklari (
                            Id int IDENTITY(1,1) PRIMARY KEY,
                            Baslik nvarchar(100) NOT NULL,
                            Konu nvarchar(200) NOT NULL,
                            Icerik nvarchar(max) NOT NULL,
                            EklenmeTarihi datetime2 NOT NULL
                        );
                    END
                    
                    IF OBJECT_ID('SistemGuncellemeleri', 'U') IS NULL
                    BEGIN
                        CREATE TABLE SistemGuncellemeleri (
                            Id int IDENTITY(1,1) PRIMARY KEY,
                            Versiyon nvarchar(20) NOT NULL,
                            Baslik nvarchar(100) NOT NULL,
                            Icerik nvarchar(max) NOT NULL,
                            EklenmeTarihi datetime2 NOT NULL
                        );
                    END
                ");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Yeni kolonlar eklenirken bir hata oluştu.");
            }

            // 1. Yönetici Hesabı Tohumlama
            if (!context.AdminUsers.Any())
            {
                context.AdminUsers.Add(new AdminUser
                {
                    KullaniciAdi = "admin",
                    AdSoyad = "Sistem Yöneticisi",
                    Rol = "Ana Yönetici",
                    AnaYonetici = true,
                    EklenmeTarihi = DateTime.Now
                });
                // Şifre hashleme vb. logic'i app startup kısmında ayarlandığı için burada sadece record ekliyoruz veya mevcut admini bırakıyoruz
                context.SaveChanges();
            }

            // Sistem Güncellemeleri için örnek veri
            if (!context.SistemGuncellemeleri.Any())
            {
                context.SistemGuncellemeleri.AddRange(
                    new SistemGuncellemesi
                    {
                        Versiyon = "v0.6",
                        Baslik = "Arayüz ve Hata Düzeltmeleri",
                        Icerik = "<ul><li>Navigasyon (üst menü) açılır listeleri daha modern, kompakt ve pürüzsüz hale getirildi.</li><li>HGS Sayfası'ndaki geçici <strong>DataTables arayüz hatası</strong> giderildi.</li><li>Arvento bağlantı testi algoritması iyileştirildi, API yanıtları JSON doğrulamasından geçirilerek daha güvenilir hale getirildi.</li></ul>",
                        EklenmeTarihi = DateTime.Now
                    },
                    new SistemGuncellemesi
                    {
                        Versiyon = "v0.5",
                        Baslik = "Altyapı Çalışmaları Başladı! 🚀",
                        Icerik = "<ul><li><strong>Canlı Arvento GPS Haritası</strong> entegrasyonu için altyapı çalışmaları ana sayfaya eklendi.</li><li><strong>HGS & Ceza Takip Paneli</strong> canlı API entegrasyonu hazırlıkları başladı.</li><li>Veritabanı desimal hesaplamalarındaki hassasiyet uyarıları (EF Core) tamamen çözüldü.</li></ul>",
                        EklenmeTarihi = DateTime.Now.AddDays(-1)
                    }
                );
                context.SaveChanges();
            }

            // 2. Araç Filosu Tohumlama (Öz Mal / Kiralık / Şehir ve Sürücü Atamaları)
            if (!context.Vehicles.Any())
            {
                context.Vehicles.AddRange(
                    new Vehicle
                    {
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
                context.SmtpAyarlari.Add(new SmtpAyari { SmtpServer = "smtp.gmail.com", Port = 587, EnableSsl = true, Aktif = false });
                context.SaveChanges();
            }

            if (!context.ArventoAyarlari.Any())
            {
                context.ArventoAyarlari.Add(new ArventoAyari
                {
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
