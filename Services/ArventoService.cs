using AracGorevFormu.Data;
using AracGorevFormu.Models;

namespace AracGorevFormu.Services
{
    public interface IArventoService
    {
        Task<bool> BaglantiyiTestEtAsync();
        Task<ArventoAracKonum?> AracKonumuGetirAsync(string plaka);
        Task<List<ArventoAracKonum>> TumAracKonumlariAsync();
        bool Yapilandirildi { get; }
        ArventoAyari AyarlariGetir();
        void AyarlariKaydet(ArventoAyari ayarlar);
    }

    public class ArventoAracKonum
    {
        public string Plaka { get; set; } = string.Empty;
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public double Hiz { get; set; }
        public DateTime SonKonumZamani { get; set; }
        public string? Adres { get; set; }
        public bool MotorAcik { get; set; }
    }

    public class ArventoService : IArventoService
    {
        private readonly ILogger<ArventoService> _logger;
        private readonly AppDbContext _db;

        public ArventoService(ILogger<ArventoService> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public bool Yapilandirildi
        {
            get
            {
                var ayar = AyarlariGetir();
                return ayar.Aktif && !string.IsNullOrEmpty(ayar.KullaniciAdi) && !string.IsNullOrEmpty(ayar.Sifre);
            }
        }

        public ArventoAyari AyarlariGetir()
        {
            var ayar = _db.ArventoAyarlari.FirstOrDefault(a => a.Id == 1);
            if (ayar == null)
            {
                ayar = new ArventoAyari { Id = 1, ApiUrl = "https://ws.arvento.com/v1/report.asmx", Aktif = false };
                _db.ArventoAyarlari.Add(ayar);
                _db.SaveChanges();
            }
            return ayar;
        }

        public void AyarlariKaydet(ArventoAyari ayarlar)
        {
            var ayar = AyarlariGetir();
            ayar.ApiUrl = ayarlar.ApiUrl;
            ayar.KullaniciAdi = ayarlar.KullaniciAdi;
            ayar.Sifre = ayarlar.Sifre;
            ayar.ApiKey = ayarlar.ApiKey;
            ayar.Aktif = ayarlar.Aktif;

            // EF Change Tracking zaten entity'yi izliyor, Update() çağırmaya gerek yok
            _db.SaveChanges();
        }

        public async Task<bool> BaglantiyiTestEtAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.ApiUrl))
            {
                return false;
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await client.GetAsync(ayar.ApiUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arvento bağlantı testi başarısız.");
                return false;
            }
        }

        public async Task<ArventoAracKonum?> AracKonumuGetirAsync(string plaka)
        {
            await Task.CompletedTask;
            return null;
        }

        public async Task<List<ArventoAracKonum>> TumAracKonumlariAsync()
        {
            await Task.CompletedTask;
            return new List<ArventoAracKonum>();
        }
    }
}
