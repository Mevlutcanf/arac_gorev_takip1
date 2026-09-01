using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
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
    }

    public class ArventoService : IArventoService
    {
        private readonly ILogger<ArventoService> _logger;
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        // WebSocket verilerini hafızada tutmak için static cache
        private static Dictionary<string, ArventoAracKonum> _konumCache = new Dictionary<string, ArventoAracKonum>();
        private static bool _isWsRunning = false;
        private static object _wsLock = new object();

        // Plaka Eşleştirme Sözlüğü (Manuel Tespit Edilenler)
        private static readonly Dictionary<string, string> _plakaEslestirme = new Dictionary<string, string>
        {
            { "1005112", "34 PHK 036" }, // Bursa
            { "1013977", "06 AT 5679" }, // Beşiktaş
            { "1017010", "06 AT 2195" }, // Ümraniye
            { "193924",  "06 FMG 424" }  // Avcılar
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
                ayar = new ArventoAyari { Id = 1, ApiUrl = "https://web.arvento.com/", Aktif = false };
                _db.ArventoAyarlari.Add(ayar);
                _db.SaveChanges();
            }
            return ayar;
        }

        public void AyarlariKaydet(ArventoAyari ayarlar)
        {
            var ayar = AyarlariGetir();
            ayar.ApiUrl = "https://web.arvento.com/"; 
            ayar.KullaniciAdi = ayarlar.KullaniciAdi;
            ayar.Sifre = ayarlar.Sifre;
            ayar.ApiKey = ayarlar.ApiKey; 
            ayar.Aktif = ayarlar.Aktif;

            _db.SaveChanges();
        }

        private async Task<string?> GetSidAsync(string username, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ArventoClient");
                client.Timeout = TimeSpan.FromSeconds(20);

                var loginData = new
                {
                    Username = username,
                    Password = password,
                    UserLanguage = "tr-TR"
                };

                var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.arvento.com/arventocom/login", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var jsonStr = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("res", out var resElem))
                {
                    var resUrl = resElem.GetString();
                    if (!string.IsNullOrEmpty(resUrl) && resUrl.Contains("sid="))
                    {
                        var sid = resUrl.Substring(resUrl.IndexOf("sid=") + 4).Split('&')[0];
                        return sid;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arvento SID alınırken hata oluştu.");
            }
            return null;
        }

        private void StartWebSocketListener(string username, string password)
        {
            lock (_wsLock)
            {
                if (_isWsRunning) return;
                _isWsRunning = true;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var sid = await GetSidAsync(username, password);
                        if (string.IsNullOrEmpty(sid))
                        {
                            await Task.Delay(10000); // 10 saniye sonra tekrar dene
                            continue;
                        }

                        using var ws = new ClientWebSocket();
                        var wsUrl = $"wss://node.arvento.com/arvento?sid={sid}&app=web2&pkt=U528%0D%0AONLN&lid={Guid.NewGuid()}&ld=1&format=json&occ=1";
                        
                        await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                        _logger.LogInformation("Arvento WebSocket canlı bağlantısı BAŞARILI!");

                        var buffer = new byte[8192];
                        while (ws.State == WebSocketState.Open)
                        {
                            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close) break;

                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            
                            // "MARK" paketleri canlı konum verileridir
                            if (message.Contains("\"p\":\"MARK\""))
                            {
                                try
                                {
                                    using var doc = JsonDocument.Parse(message);
                                    var root = doc.RootElement;
                                    
                                    if (root.TryGetProperty("n", out var nodeProp))
                                    {
                                        var nodeId = nodeProp.GetString();
                                        
                                        if (root.TryGetProperty("x", out var xProp) && root.TryGetProperty("y", out var yProp))
                                        {
                                            double lat = yProp.GetDouble(); // y = lat
                                            double lon = xProp.GetDouble(); // x = lon
                                            
                                            double speed = 0;
                                            if (root.TryGetProperty("s", out var sProp)) speed = sProp.GetDouble();
                                            
                                            string address = "";
                                            if (root.TryGetProperty("ad", out var adProp)) address = adProp.GetString();

                                            // Plakayı eşleştirme sözlüğünden al, yoksa "Araç {NodeID}" olarak bırak
                                            string plakaYazisi = _plakaEslestirme.ContainsKey(nodeId) ? _plakaEslestirme[nodeId] : ("Araç " + nodeId);

                                            var konum = new ArventoAracKonum
                                            {
                                                Plaka = plakaYazisi,
                                                Enlem = lat,
                                                Boylam = lon,
                                                Hiz = speed,
                                                Adres = address,
                                                SonKonumZamani = DateTime.Now
                                            };

                                            lock (_wsLock)
                                            {
                                                _konumCache[nodeId] = konum;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("WebSocket JSON parse hatası: " + ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Arvento WebSocket koptu veya hata verdi: " + ex.Message);
                        await Task.Delay(5000); // Yeniden bağlanmadan önce bekle
                    }
                }
            });
        }

        public async Task<bool> BaglantiyiTestEtAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.KullaniciAdi) || string.IsNullOrEmpty(ayar.Sifre)) return false;

            var sid = await GetSidAsync(ayar.KullaniciAdi, ayar.Sifre);
            return !string.IsNullOrEmpty(sid);
        }

        public async Task<ArventoAracKonum?> AracKonumuGetirAsync(string plaka)
        {
            var tumu = await TumAracKonumlariAsync();
            return tumu.FirstOrDefault(a => a.Plaka != null && a.Plaka.Replace(" ", "").Equals(plaka.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<ArventoAracKonum>> TumAracKonumlariAsync()
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrEmpty(ayar.KullaniciAdi) || string.IsNullOrEmpty(ayar.Sifre)) 
                return new List<ArventoAracKonum>();

            if (!_isWsRunning)
            {
                StartWebSocketListener(ayar.KullaniciAdi, ayar.Sifre);
                
                // İlk defa başlıyorsa verilerin dolması için 2 saniye bekleyelim
                await Task.Delay(2000);
            }

            // 1. Veritabanından ŞasiNo alanına Node ID girilmiş araçları alalım
            var dbAraclar = _db.Vehicles
                .Where(v => !string.IsNullOrEmpty(v.SasiNo))
                .Select(v => new { v.SasiNo, v.Plaka })
                .ToList();

            // 2. Cache'teki konumların bir kopyasını (referansını bozmadan) alalım
            List<ArventoAracKonum> konumlar;
            lock (_wsLock)
            {
                konumlar = _konumCache.Values.Select(k => new ArventoAracKonum
                {
                    Plaka = k.Plaka,
                    Enlem = k.Enlem,
                    Boylam = k.Boylam,
                    Hiz = k.Hiz,
                    Adres = k.Adres,
                    SonKonumZamani = k.SonKonumZamani
                }).ToList();
            }

            // 3. Eğer plaka "Araç XXX" şeklindeyse ve DB'de ŞasiNo karşılığı varsa plakayı güncelle
            foreach (var konum in konumlar)
            {
                if (konum.Plaka != null && konum.Plaka.StartsWith("Araç "))
                {
                    var nodeId = konum.Plaka.Replace("Araç ", "").Trim();
                    var eslesme = dbAraclar.FirstOrDefault(v => v.SasiNo == nodeId);
                    if (eslesme != null)
                    {
                        konum.Plaka = eslesme.Plaka; // Veritabanındaki gerçek plakayı atıyoruz!
                    }
                }
            }

            return konumlar;
        }
    }
}
