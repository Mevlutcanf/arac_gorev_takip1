using System.Net.Http.Headers;
using System.Text.Json;
using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;

namespace AracGorevFormu.Services
{
    public interface IHgsService
    {
        Task<List<HgsGecis>> PlakaGecisleriniGetirAsync(string plaka);
        Task<decimal> PlakaToplamBorcuGetirAsync(string plaka);
        Task<HgsBorcOzet> BorcSorgulaAsync(string plaka);
        Task<FiloHgsDashboardViewModel> FiloHgsOzetiGetirAsync(List<Vehicle> tumAraclar, string? seciliPlaka = null);
    }

    public class HgsBorcOzet
    {
        public string Plaka { get; set; } = string.Empty;
        public decimal OdenmeyenTutar { get; set; }
        public int OdenmeyenAdet { get; set; }
        public int CezaAdet { get; set; }
        public bool ApiKullanildi { get; set; } = false;
        public string? Mesaj { get; set; }
        public List<HgsGecis> Gecisler { get; set; } = new();
    }

    public class HgsService : IHgsService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<HgsService> _logger;
        private readonly IConfiguration _config;
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("https://api.test.isbank.com.tr/api/sandbox-isbank/hgs/v1/"), Timeout = TimeSpan.FromSeconds(10) };

        public HgsService(AppDbContext db, ILogger<HgsService> logger, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _config = config;
        }

        private static string NormalizePlaka(string? plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka)) return string.Empty;
            return plaka.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        }

        public async Task<List<HgsGecis>> PlakaGecisleriniGetirAsync(string plaka)
        {
            var ozet = await BorcSorgulaAsync(plaka);
            return ozet.Gecisler;
        }

        public async Task<decimal> PlakaToplamBorcuGetirAsync(string plaka)
        {
            var ozet = await BorcSorgulaAsync(plaka);
            return ozet.OdenmeyenTutar;
        }

        public async Task<HgsBorcOzet> BorcSorgulaAsync(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka)) return new HgsBorcOzet();

            var plakaFormatted = plaka.Trim().ToUpperInvariant();
            var normalizedPlaka = NormalizePlaka(plaka);

            // Son 15 gün aralığı
            var startDate = DateTime.Now.AddDays(-15).ToString("yyyy-MM-dd");
            var finishDate = DateTime.Now.ToString("yyyy-MM-dd");

            // 1. Canlı İşbank HGS API Entegrasyon Çağrısı (Sadece GERÇEK API Yanıtı Çekilir)
            var arventoAyar = _db.ArventoAyarlari.FirstOrDefault(a => a.Id == 1);
            var apiToken = _config["HgsApi:BearerToken"] ?? arventoAyar?.ApiKey;
            var clientId = _config["HgsApi:ClientId"] ?? arventoAyar?.KullaniciAdi ?? "";
            var clientSecret = _config["HgsApi:ClientSecret"] ?? arventoAyar?.Sifre ?? "";

            bool apiBasarili = false;
            string apiHataMesaji = "";

            if (!string.IsNullOrEmpty(apiToken) || !string.IsNullOrEmpty(clientId))
            {
                try
                {
                    var endpoint = $"query/transits-by-plate?plate={normalizedPlaka}&start-date={startDate}&finish-date={finishDate}&page-no=1&page-size=100";
                    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    if (!string.IsNullOrEmpty(apiToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
                    if (!string.IsNullOrEmpty(clientId))
                        request.Headers.Add("X-Isbank-Client-Id", clientId);
                    if (!string.IsNullOrEmpty(clientSecret))
                        request.Headers.Add("X-Isbank-Client-Secret", clientSecret);

                    request.Headers.Add("Accept-Language", "tr");

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        ParseAndSaveIsbankTransits(json, plakaFormatted);
                        apiBasarili = true;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        apiHataMesaji = "Yetkilendirme hatası (401/403). HGS API anahtarı geçersiz.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        apiHataMesaji = "Plaka banka/HGS sisteminde bulunamadı veya geçersiz format.";
                    }
                    else
                    {
                        apiHataMesaji = $"API servisi geçici olarak kullanılamıyor (HTTP {(int)response.StatusCode}).";
                    }
                }
                catch (TaskCanceledException)
                {
                    apiHataMesaji = "API sorgusu zaman aşımına uğradı (Timeout).";
                }
                catch (HttpRequestException)
                {
                    apiHataMesaji = "API sunucusuna erişilemiyor (Network/DNS hatası).";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "İşbank HGS API bağlantısı sırasında uyarı oluştu.");
                    apiHataMesaji = "Beklenmeyen bir API entegrasyon hatası oluştu.";
                }
            }
            else
            {
                apiHataMesaji = "Sistemde HGS API kimlik bilgileri (Client ID/Secret) yapılandırılmamış.";
            }

            // 2. SADECE Gerçek Veritabanı Kayıtları (Sahte/Simülasyon veri tamamen kaldırıldı)
            var tumHgsKayıtlari = _db.HgsGecisleri.ToList();
            var yerelGecisler = tumHgsKayıtlari
                .Where(h => NormalizePlaka(h.Plaka) == normalizedPlaka)
                .OrderByDescending(h => h.GecisTarihi)
                .ToList();

            var odenmeyenler = yerelGecisler.Where(g => !g.OdediMi).ToList();

            return new HgsBorcOzet
            {
                Plaka = plakaFormatted,
                Gecisler = yerelGecisler,
                OdenmeyenTutar = odenmeyenler.Sum(g => g.Tutar),
                OdenmeyenAdet = odenmeyenler.Count,
                CezaAdet = odenmeyenler.Count(g => g.CezaMi),
                ApiKullanildi = apiBasarili,
                Mesaj = apiBasarili
                    ? $"✅ İşbank HGS API: {plakaFormatted} plakası için gerçek geçişler başarıyla çekildi."
                    : $"⚠️ API Hatası: {apiHataMesaji} (Ekranda sadece geçmiş veritabanı kayıtları gösterilmektedir.)"
            };
        }

        public async Task<FiloHgsDashboardViewModel> FiloHgsOzetiGetirAsync(List<Vehicle> tumAraclar, string? seciliPlaka = null)
        {
            var tumGecisler = _db.HgsGecisleri.ToList().OrderByDescending(g => g.GecisTarihi).ToList();

            if (!string.IsNullOrWhiteSpace(seciliPlaka))
            {
                var norm = NormalizePlaka(seciliPlaka);
                tumGecisler = tumGecisler.Where(g => NormalizePlaka(g.Plaka) == norm).ToList();
            }

            var odenmeyenGecisler = tumGecisler.Where(g => !g.OdediMi).ToList();

            var model = new FiloHgsDashboardViewModel
            {
                ToplamFiloBorcu = odenmeyenGecisler.Sum(g => g.Tutar),
                ToplamOdenmeyenGecisSayisi = odenmeyenGecisler.Count,
                ToplamCezaSayisi = odenmeyenGecisler.Count(g => g.CezaMi),
                ToplamAracSayisi = tumAraclar.Count,
                TumGecisler = tumGecisler,
                SeciliPlaka = seciliPlaka
            };

            var tumHgsListesi = _db.HgsGecisleri.ToList();

            foreach (var arac in tumAraclar)
            {
                var normPlaka = NormalizePlaka(arac.Plaka);
                var aracGecisleri = tumHgsListesi
                    .Where(g => NormalizePlaka(g.Plaka) == normPlaka)
                    .OrderByDescending(g => g.GecisTarihi)
                    .ToList();

                var odenmeyen = aracGecisleri.Where(g => !g.OdediMi).ToList();

                model.AracOzetleri.Add(new AracHgsOzetViewModel
                {
                    VehicleId = arac.Id,
                    Plaka = arac.Plaka,
                    MarkaModel = $"{arac.Marka} {arac.Model}",
                    Sürücü = arac.SabitSurucu,
                    Lokasyon = arac.Lokasyon,
                    ToplamBorc = odenmeyen.Sum(g => g.Tutar),
                    OdenmeyenAdet = odenmeyen.Count,
                    CezaAdet = odenmeyen.Count(g => g.CezaMi),
                    Gecisler = aracGecisleri
                });
            }

            model.BorcluAracSayisi = model.AracOzetleri.Count(a => a.ToplamBorc > 0);

            await Task.CompletedTask;
            return model;
        }

        private void ParseAndSaveIsbankTransits(string json, string plaka)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                JsonElement arrayElement = default;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = root;
                }
                else if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = dataProp;
                }
                else if (root.TryGetProperty("transits", out var transitsProp) && transitsProp.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = transitsProp;
                }

                if (arrayElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arrayElement.EnumerateArray())
                    {
                        var gise = item.TryGetProperty("stationName", out var s) ? s.GetString() : "Otoyol Geçiş Gişesi";
                        var tutar = item.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0m;
                        var ceza = item.TryGetProperty("isPenalty", out var p) && p.GetBoolean();
                        var tarih = item.TryGetProperty("transitDate", out var d) && d.TryGetDateTime(out var dt) ? dt : DateTime.Now;

                        var varMi = _db.HgsGecisleri.Any(g => NormalizePlaka(g.Plaka) == NormalizePlaka(plaka) && g.GiseAdı == gise && g.GecisTarihi == tarih);
                        if (!varMi)
                        {
                            _db.HgsGecisleri.Add(new HgsGecis
                            {
                                Plaka = plaka,
                                GiseAdı = gise ?? "Otoyol Geçiş Gişesi",
                                Tutar = tutar,
                                CezaMi = ceza,
                                GecisTarihi = tarih,
                                OdediMi = false
                            });
                        }
                    }
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İşbank HGS JSON parse hatası");
            }
        }
    }
}
