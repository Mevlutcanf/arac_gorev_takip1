using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.Extensions.DependencyInjection;

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
        public int? ToplamKm { get; set; }
    }

    public class ArventoService : IArventoService
    {
        private readonly ILogger<ArventoService> _logger;
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        // API İsteklerini yormamak için basit bellek içi önbellek (Cache)
        private static List<ArventoAracKonum> _konumCache = new List<ArventoAracKonum>();
        private static DateTime _lastFetchTime = DateTime.MinValue;
        private static readonly object _cacheLock = new object();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        // Plaka Eşleştirme Sözlüğü
        private static readonly Dictionary<string, string> _plakaEslestirme = new Dictionary<string, string>
        {
            { "1005112", "34 PHK 036" }, 
            { "1013977", "06 AT 5679" }, 
            { "1017010", "06 AT 2195" }, 
            { "193924",  "06 FMG 424" }  
        };

        public ArventoService(ILogger<ArventoService> logger, AppDbContext db, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _db = db;
            _httpClientFactory = httpClientFactory;
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
            ayar.ApiUrl = string.IsNullOrEmpty(ayarlar.ApiUrl) ? "https://ws.arvento.com/v1/report.asmx" : ayarlar.ApiUrl; 
            ayar.KullaniciAdi = ayarlar.KullaniciAdi; // Genelde PIN1
            ayar.Sifre = ayarlar.Sifre; // Genelde PIN2
            ayar.ApiKey = ayarlar.ApiKey; 
            ayar.Aktif = ayarlar.Aktif;

            _db.SaveChanges();
        }

        public async Task<bool> BaglantiyiTestEtAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.KullaniciAdi) || string.IsNullOrEmpty(ayar.Sifre)) return false;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", ayar.KullaniciAdi),
                    new KeyValuePair<string, string>("PIN1", ayar.Sifre),
                    new KeyValuePair<string, string>("PIN2", ayar.ApiKey)
                });
                
                var requestUrl = ayar.ApiUrl.TrimEnd('/') + "/GetVehicleStatusJSON";
                var response = await client.PostAsync(requestUrl, content);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arvento API Baglanti Testi Basarisiz");
                return false;
            }
        }

        public async Task<ArventoAracKonum?> AracKonumuGetirAsync(string plaka)
        {
            var tumu = await TumAracKonumlariAsync();
            return tumu.FirstOrDefault(a => a.Plaka != null && a.Plaka.Replace(" ", "").Equals(plaka.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }

        private async Task FetchFromApiAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.KullaniciAdi) || string.IsNullOrEmpty(ayar.Sifre)) return;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", ayar.KullaniciAdi),
                    new KeyValuePair<string, string>("PIN1", ayar.Sifre),
                    new KeyValuePair<string, string>("PIN2", ayar.ApiKey)
                });

                // JSON yanıt veren endpoint
                var requestUrl = ayar.ApiUrl.TrimEnd('/') + "/GetVehicleStatusJSON";
                var response = await client.PostAsync(requestUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonStr = await response.Content.ReadAsStringAsync();
                    var newKonumlar = new List<ArventoAracKonum>();

                    // API bazen HTML hata sayfası döndürür (yanlış kimlik bilgileri vb.)
                    // JSON olmayan yanıtları filtrele
                    var trimmed = jsonStr.TrimStart();
                    if (string.IsNullOrWhiteSpace(trimmed) || (!trimmed.StartsWith("[") && !trimmed.StartsWith("{")))
                    {
                        // İlk 200 karakteri logla (debug için)
                        var snippet = trimmed.Length > 200 ? trimmed.Substring(0, 200) : trimmed;
                        _logger.LogWarning("Arvento API JSON yerine beklenmeyen yanıt döndürdü. Başlangıç: {Snippet}", snippet);
                        return; // JSON değilse parse etme
                    }

                    try
                    {
                        using var doc = JsonDocument.Parse(jsonStr);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in doc.RootElement.EnumerateArray())
                            {
                                var konum = new ArventoAracKonum { SonKonumZamani = DateTime.Now };

                                if (item.TryGetProperty("Node", out var nodeProp))
                                    konum.Plaka = _plakaEslestirme.ContainsKey(nodeProp.GetString() ?? "") ? _plakaEslestirme[nodeProp.GetString()!] : ("Araç " + nodeProp.GetString());
                                else if (item.TryGetProperty("Plate", out var plateProp))
                                    konum.Plaka = plateProp.GetString() ?? "Bilinmiyor";

                                if (item.TryGetProperty("Latitude", out var latProp) && latProp.TryGetDouble(out var lat))
                                    konum.Enlem = lat;
                                
                                if (item.TryGetProperty("Longitude", out var lonProp) && lonProp.TryGetDouble(out var lon))
                                    konum.Boylam = lon;

                                if (item.TryGetProperty("Speed", out var speedProp) && speedProp.TryGetDouble(out var speed))
                                    konum.Hiz = speed;

                                if (item.TryGetProperty("TotalDistance", out var distProp) && distProp.TryGetInt32(out var dist))
                                    konum.ToplamKm = dist;
                                else if (item.TryGetProperty("Distance", out var dist2Prop) && dist2Prop.TryGetInt32(out var dist2))
                                    konum.ToplamKm = dist2;
                                
                                if (item.TryGetProperty("Address", out var adrProp))
                                    konum.Adres = adrProp.GetString();
                                
                                if (item.TryGetProperty("Date", out var dateProp) && DateTime.TryParse(dateProp.GetString(), out var parsedDate))
                                    konum.SonKonumZamani = parsedDate;

                                newKonumlar.Add(konum);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("JSON parse hatası. Hata: " + ex.Message);
                    }
                    
                    if (newKonumlar.Count > 0)
                    {
                        lock (_cacheLock)
                        {
                            _konumCache = newKonumlar;
                            _lastFetchTime = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arvento API'den veri çekerken hata oluştu.");
            }
        }

        public async Task<List<ArventoAracKonum>> TumAracKonumlariAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.KullaniciAdi) || string.IsNullOrEmpty(ayar.Sifre)) 
                return new List<ArventoAracKonum>();

            // Cache kontrolü - Veriler 60 saniyeden eskiyse yeniden API isteği at
            bool needsFetch = false;
            lock (_cacheLock)
            {
                if ((DateTime.Now - _lastFetchTime) > CacheDuration)
                {
                    needsFetch = true;
                }
            }

            if (needsFetch)
            {
                await FetchFromApiAsync();
            }

            List<ArventoAracKonum> konumlar;
            lock (_cacheLock)
            {
                konumlar = _konumCache.ToList(); // Kopyasını al
            }

            // DB ile senkronizasyon (Güncel KM, Adres vs.)
            var dbAraclar = _db.Vehicles
                .Where(v => !string.IsNullOrEmpty(v.SasiNo) || !string.IsNullOrEmpty(v.Plaka))
                .ToList();

            bool dbUpdated = false;

            foreach (var konum in konumlar)
            {
                Vehicle? eslesme = null;
                if (konum.Plaka != null && konum.Plaka.StartsWith("Araç "))
                {
                    var nodeId = konum.Plaka.Replace("Araç ", "").Trim();
                    eslesme = dbAraclar.FirstOrDefault(v => v.SasiNo == nodeId);
                    if (eslesme != null)
                        konum.Plaka = eslesme.Plaka; // Gerçek plaka ataması
                }
                else if (!string.IsNullOrEmpty(konum.Plaka))
                {
                    eslesme = dbAraclar.FirstOrDefault(v => v.Plaka != null && v.Plaka.Replace(" ", "").Equals(konum.Plaka.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                }

                if (eslesme != null)
                {
                    bool change = false;
                    
                    if (konum.ToplamKm.HasValue && eslesme.GuncelKm != konum.ToplamKm.Value)
                    {
                        eslesme.GuncelKm = konum.ToplamKm.Value;
                        change = true;
                    }
                    
                    if (!string.IsNullOrEmpty(konum.Adres) && eslesme.SonAdres != konum.Adres)
                    {
                        eslesme.SonAdres = konum.Adres;
                        change = true;
                    }

                    if (eslesme.SonKonumZamani == null || (konum.SonKonumZamani - eslesme.SonKonumZamani.Value).TotalMinutes > 1)
                    {
                        eslesme.SonKonumZamani = konum.SonKonumZamani;
                        change = true;
                    }

                    if (change) dbUpdated = true;
                }
            }

            if (dbUpdated)
            {
                _db.SaveChanges();
            }

            return konumlar;
        }
    }
}
