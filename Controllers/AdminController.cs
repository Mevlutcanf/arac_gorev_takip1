using System.Security.Claims;
using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly VehicleRepository _vehicleRepo;
        private readonly GorevFormuRepository _formRepo;
        private readonly AdminUserRepository _adminRepo;
        private readonly ArventoService _arventoService;
        private readonly IEmailService _emailService;
        private readonly IHgsService _hgsService;
        private readonly AppDbContext _db;

        public AdminController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo,
            AdminUserRepository adminRepo, ArventoService arventoService, IEmailService emailService,
            IHgsService hgsService, AppDbContext db)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _adminRepo = adminRepo;
            _arventoService = arventoService;
            _emailService = emailService;
            _hgsService = hgsService;
            _db = db;
        }

        private string MevcutKullaniciAdi => User.Identity?.Name ?? "Bilinmiyor";
        private int MevcutKullaniciId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private string GetClientIpAddress()
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip)) ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ip == "::1" || ip == "127.0.0.1") return "Localhost";
            return string.IsNullOrEmpty(ip) ? "Bilinmiyor" : ip;
        }

        private async Task LogIslemAsync(string islemTuru, string detay)
        {
            var log = new SystemLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = MevcutKullaniciAdi,
                IslemTuru = islemTuru,
                Detay = detay,
                IpAdresi = GetClientIpAddress()
            };
            _db.SystemLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        // ---------------- DASHBOARD ----------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tumFormlar = await _formRepo.TumuAsync();
            var araclar = await _vehicleRepo.TumuAsync();
            var disaridakiFormlar = tumFormlar.Where(f => f.AracDisarida).ToList();
            var disaridakiAracIdleri = disaridakiFormlar.Select(f => f.VehicleId).ToHashSet();

            var arventoData = await _arventoService.TumAracKonumlariAsync();
            var gercekDisaridakiFormlar = new List<GorevFormu>();
            var gercekIceridekiAraclar = new List<Vehicle>();

            foreach(var arac in araclar.Where(a => a.Aktif))
            {
                var arventoMatch = arventoData.FirstOrDefault(x => x.Plaka == arac.Plaka);
                
                if (arventoMatch != null)
                {
                    string adresLower = (arventoMatch.Adres ?? "").ToLowerInvariant();
                    bool sirkette = adresLower.Contains("altınordu") || adresLower.Contains("altinordu") || 
                               adresLower.Contains("abdurrahman tatlıcı") || adresLower.Contains("abdurrahman tatlici");
                               
                    if (sirkette)
                    {
                        gercekIceridekiAraclar.Add(arac);
                    }
                    else
                    {
                        var form = disaridakiFormlar.FirstOrDefault(f => f.VehicleId == arac.Id);
                        if (form != null)
                        {
                            gercekDisaridakiFormlar.Add(form);
                        }
                        else
                        {
                            gercekDisaridakiFormlar.Add(new GorevFormu {
                                AracPlaka = arac.Plaka,
                                CikisZamani = arventoMatch.SonKonumZamani,
                                KullananAdSoyad = "Bilinmiyor (GPS Tespit)",
                                GorevAmaci = (arventoMatch.Adres) ?? "Canlı Arvento Tespiti"
                            });
                        }
                    }
                }
                else
                {
                    // Arvento eşleşmesi yoksa, DB'de görevi olanları 'Dışarıda', görevi olmayanları 'Bilinmiyor/Beklemede' sayacağız.
                    // Şimdilik listeleri kirletmemesi adına 'Şirkette' listesine de, 'Dışarıda' listesine de EKLEMİYORUZ 
                    // Yalnızca DB'de aktif bir görevi varsa onu Dışarıda listesine ekliyoruz.
                    var form = disaridakiFormlar.FirstOrDefault(f => f.VehicleId == arac.Id);
                    if (form != null)
                    {
                        gercekDisaridakiFormlar.Add(form);
                    }
                }
            }

            var model = new AdminDashboardViewModel
            {
                ToplamArac = araclar.Count,
                AktifArac = araclar.Count(a => a.Aktif),
                SuAndaDisaridaOlan = gercekDisaridakiFormlar.Count,
                SuAndaIcerideOlan = gercekIceridekiAraclar.Count,
                BekleyenOnaySayisi = tumFormlar.Count(f => f.Durum == GorevDurumu.Beklemede),
                BugunCikanSayisi = tumFormlar.Count(f => f.OlusturmaTarihi.Date == DateTime.Today),
                SonFormlar = tumFormlar.Take(10).ToList(),
                DisaridakiAraclar = gercekDisaridakiFormlar,
                IceridekiAraclar = gercekIceridekiAraclar
            };

            return View(model);
        }

        // ---------------- GÖREV FORMLARI ----------------

        [HttpGet]
        public async Task<IActionResult> Formlar(string? durum)
        {
            var formlar = await _formRepo.TumuAsync();

            if (!string.IsNullOrEmpty(durum) && Enum.TryParse<GorevDurumu>(durum, out var durumFiltre))
            {
                formlar = formlar.Where(f => f.Durum == durumFiltre).ToList();
            }

            ViewBag.SeciliDurum = durum;
            return View(formlar);
        }

        [HttpGet]
        public async Task<IActionResult> FormDetay(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form == null) return NotFound();
            return View(form);
        }

        /// <summary>
        /// Resmi Mevzuata Uyumlu Araç Görev ve Teslim Tutanağı (Baskı & Islak İmza Formatı)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResmiTutanak(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormOnayla(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Beklemede)
            {
                form.Durum = GorevDurumu.Onaylandi;
                form.OnaylayanKullaniciAdi = MevcutKullaniciAdi;
                form.OnayTarihi = DateTime.Now;

                // Aracın anlık konumunu ve KM'sini çekip Çıkış KM'si olarak kaydet
                var arac = await _vehicleRepo.GetirByIdAsync(form.VehicleId);
                if (arac != null)
                {
                    var anlikKonum = await _arventoService.AracKonumuGetirAsync(arac.Plaka);
                    if (anlikKonum != null && anlikKonum.ToplamKm.HasValue)
                    {
                        form.CikisKm = anlikKonum.ToplamKm.Value;
                    }
                    else if (arac.GuncelKm.HasValue)
                    {
                        form.CikisKm = arac.GuncelKm.Value;
                    }
                }

                await _formRepo.GuncelleAsync(form);
                await _emailService.FormDurumDegisiklikBildirimiGonderAsync(form, onaylandi: true);
                await LogIslemAsync("Form Onaylandı", $"{form.AracPlaka} plakalı aracın görev formu onaylandı.");
                TempData["Mesaj"] = $"{form.AracPlaka} plakalı araç için görev formu onaylandı.";
            }

            return RedirectToAction(nameof(ResmiTutanak), new { id = form.Id });
        }

        [HttpGet]
        public async Task<IActionResult> FormReddet(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form == null) return NotFound();
            return View(new RedViewModel { FormId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormReddet(RedViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var form = await _formRepo.GetirByIdAsync(model.FormId);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Beklemede)
            {
                form.Durum = GorevDurumu.Reddedildi;
                form.OnaylayanKullaniciAdi = MevcutKullaniciAdi;
                form.OnayTarihi = DateTime.Now;
                form.RedNedeni = model.RedNedeni;
                await _formRepo.GuncelleAsync(form);
                await _emailService.FormDurumDegisiklikBildirimiGonderAsync(form, onaylandi: false);
                await LogIslemAsync("Form Reddedildi", $"{form.AracPlaka} plakalı aracın görev formu reddedildi. Neden: {model.RedNedeni}");
                TempData["Mesaj"] = "Görev formu reddedildi.";
            }

            return RedirectToAction(nameof(Formlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormDonusIsaretle(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Onaylandi && form.GercekDonusZamani == null)
            {
                form.GercekDonusZamani = DateTime.Now;
                form.Durum = GorevDurumu.TamamlandiDondu;

                // Aracın anlık konumunu ve KM'sini çekip Dönüş KM'si olarak kaydet
                var arac = await _vehicleRepo.GetirByIdAsync(form.VehicleId);
                if (arac != null)
                {
                    var anlikKonum = await _arventoService.AracKonumuGetirAsync(arac.Plaka);
                    if (anlikKonum != null && anlikKonum.ToplamKm.HasValue)
                    {
                        form.DonusKm = anlikKonum.ToplamKm.Value;
                    }
                    else if (arac.GuncelKm.HasValue)
                    {
                        form.DonusKm = arac.GuncelKm.Value;
                    }
                }

                await _formRepo.GuncelleAsync(form);
                
                // Araç iade edildi e-postası gönder
                await _emailService.FormTamamlandiBildirimiGonderAsync(form);
                
                await LogIslemAsync("Araç Döndü", $"{form.AracPlaka} plakalı aracın dönüşü (görevin tamamlanması) kaydedildi.");
                TempData["Mesaj"] = $"{form.AracPlaka} plakalı aracın dönüşü kaydedildi.";
            }

            return RedirectToAction(nameof(Formlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormSil(int id)
        {
            var form = await _formRepo.GetirByIdAsync(id);
            if (form != null)
            {
                var takipKodu = form.TakipKodu;
                await _formRepo.SilAsync(id);
                await LogIslemAsync("Görev Formu Silindi", $"{takipKodu} numaralı görev formu sistemden silindi.");
                TempData["Mesaj"] = "Görev formu başarıyla silindi.";
            }
            return RedirectToAction(nameof(Formlar));
        }

        // ---------------- ARAÇ VE RUHSAT YÖNETİMİ ----------------

        [HttpGet]
        public async Task<IActionResult> Araclar()
        {
            return View(await _vehicleRepo.TumuAsync());
        }

        [HttpGet]
        public async Task<IActionResult> AracDetay(int id)
        {
            var arac = await _vehicleRepo.GetirByIdAsync(id);
            if (arac == null) return NotFound();

            var formlar = (await _formRepo.TumuAsync()).Where(f => f.VehicleId == id).OrderByDescending(f => f.OlusturmaTarihi).ToList();
            var bakimlar = await _db.AracBakimlari.Where(b => b.VehicleId == id).OrderByDescending(b => b.BakimTarihi).ToListAsync();
            var hgs = await _db.HgsGecisleri.Where(h => h.Plaka == arac.Plaka).OrderByDescending(h => h.GecisTarihi).ToListAsync();

            var model = new AracDetayViewModel
            {
                Arac = arac,
                SonGorevler = formlar,
                BakimGecmisi = bakimlar,
                HgsGecisleri = hgs
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult AracEkle()
        {
            return View(new Vehicle { Aktif = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracEkle(Vehicle vehicle)
        {
            if (await _vehicleRepo.PlakaVarMiAsync(vehicle.Plaka))
            {
                ModelState.AddModelError(nameof(vehicle.Plaka), "Bu plaka zaten kayıtlı.");
            }

            if (!ModelState.IsValid) return View(vehicle);

            await _vehicleRepo.EkleAsync(vehicle);
            await LogIslemAsync("Araç Eklendi", $"{vehicle.Plaka} plakalı yeni araç filoya eklendi.");
            TempData["Mesaj"] = "Araç başarıyla eklendi.";
            return RedirectToAction(nameof(Araclar));
        }

        [HttpGet]
        public async Task<IActionResult> AracDuzenle(int id)
        {
            var arac = await _vehicleRepo.GetirByIdAsync(id);
            if (arac == null) return NotFound();
            return View(arac);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracDuzenle(Vehicle vehicle, IFormFile? ruhsatDosya)
        {
            if (await _vehicleRepo.PlakaVarMiAsync(vehicle.Plaka, vehicle.Id))
            {
                ModelState.AddModelError(nameof(vehicle.Plaka), "Bu plaka başka bir araca kayıtlı.");
            }

            if (!ModelState.IsValid) return View(vehicle);

            // Ruhsat Dosyası Yükleme — Veritabanına kaydet (diske değil)
            if (ruhsatDosya != null && ruhsatDosya.Length > 0)
            {
                using var ms = new MemoryStream();
                await ruhsatDosya.CopyToAsync(ms);

                vehicle.RuhsatDosyaIcerigi = ms.ToArray();
                vehicle.RuhsatDosyaAdi = ruhsatDosya.FileName;
                vehicle.RuhsatDosyaTipi = ruhsatDosya.ContentType;
                vehicle.RuhsatDosyaYolu = null; // Artık disk yolu kullanılmıyor
            }
            else
            {
                // Dosya yüklenmemişse mevcut DB'deki dosyayı koru
                var mevcutArac = await _vehicleRepo.GetirByIdAsync(vehicle.Id);
                if (mevcutArac != null)
                {
                    vehicle.RuhsatDosyaIcerigi = mevcutArac.RuhsatDosyaIcerigi;
                    vehicle.RuhsatDosyaAdi = mevcutArac.RuhsatDosyaAdi;
                    vehicle.RuhsatDosyaTipi = mevcutArac.RuhsatDosyaTipi;
                    vehicle.RuhsatDosyaYolu = mevcutArac.RuhsatDosyaYolu;
                }
            }

            await _vehicleRepo.GuncelleAsync(vehicle);
            await LogIslemAsync("Araç Güncellendi", $"{vehicle.Plaka} plakalı aracın bilgileri güncellendi.");
            TempData["Mesaj"] = "Araç ve ruhsat bilgileri güncellendi.";
            return RedirectToAction(nameof(Araclar));
        }

        /// <summary>
        /// Ruhsat dosyasını veritabanından indirmek için endpoint
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RuhsatDosyaIndir(int id)
        {
            var arac = await _vehicleRepo.GetirByIdAsync(id);
            if (arac == null || arac.RuhsatDosyaIcerigi == null || arac.RuhsatDosyaIcerigi.Length == 0)
                return NotFound();

            return File(arac.RuhsatDosyaIcerigi, arac.RuhsatDosyaTipi ?? "application/octet-stream", arac.RuhsatDosyaAdi ?? $"Ruhsat_{arac.Plaka}.dat");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracSil(int id)
        {
            var tumFormlar = await _formRepo.TumuAsync();
            var kullanimda = tumFormlar.Any(f => f.VehicleId == id && f.AracDisarida);
            if (kullanimda)
            {
                TempData["Hata"] = "Bu araç şu anda kullanımda (dışarıda) olduğu için silinemez.";
                return RedirectToAction(nameof(Araclar));
            }

            var target = await _vehicleRepo.GetirByIdAsync(id);
            var aracSilPlaka = target?.Plaka ?? "Bilinmeyen";
            await _vehicleRepo.SilAsync(id);
            await LogIslemAsync("Araç Silindi", $"{aracSilPlaka} plakalı araç silindi.");
            TempData["Mesaj"] = "Araç silindi.";
            return RedirectToAction(nameof(Araclar));
        }

        // ---------------- ARAÇ BAKIM VE SERVİS TAKİBİ ----------------

        [HttpGet]
        public async Task<IActionResult> Bakimlar(int? vehicleId, DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            var bakimlar = _db.AracBakimlari.OrderByDescending(b => b.BakimTarihi).AsQueryable();
            
            if (vehicleId != null)
            {
                bakimlar = bakimlar.Where(b => b.VehicleId == vehicleId);
            }
            if (baslangicTarihi != null)
            {
                bakimlar = bakimlar.Where(b => b.BakimTarihi >= baslangicTarihi);
            }
            if (bitisTarihi != null)
            {
                // Set the end date to the end of the day to include all records on that date
                var bTarihi = bitisTarihi.Value.Date.AddDays(1).AddTicks(-1);
                bakimlar = bakimlar.Where(b => b.BakimTarihi <= bTarihi);
            }

            ViewBag.Araclar = await _vehicleRepo.TumuAsync();
            ViewBag.SeciliVehicleId = vehicleId;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            
            return View(await bakimlar.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BakimEkle(AracBakim bakim)
        {
            var arac = await _vehicleRepo.GetirByIdAsync(bakim.VehicleId);
            if (arac == null)
            {
                TempData["Hata"] = "Geçersiz araç seçim.";
                return RedirectToAction(nameof(Bakimlar));
            }

            bakim.Plaka = arac.Plaka;
            bakim.EklenmeTarihi = DateTime.Now;

            _db.AracBakimlari.Add(bakim);
            await _db.SaveChangesAsync();

            await LogIslemAsync("Bakım Eklendi", $"{arac.Plaka} plakalı araç için {bakim.BakimTuru} eklendi.");
            TempData["Mesaj"] = $"{arac.Plaka} plakalı araç için bakım kaydı eklendi.";
            return RedirectToAction(nameof(Bakimlar));
        }

        [HttpGet]
        public async Task<IActionResult> BakimDuzenle(int id)
        {
            var bakim = await _db.AracBakimlari.FirstOrDefaultAsync(b => b.Id == id);
            if (bakim == null) return NotFound();
            
            ViewBag.Araclar = await _vehicleRepo.TumuAsync();
            return View(bakim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BakimDuzenle(AracBakim model)
        {
            var mevcut = await _db.AracBakimlari.FirstOrDefaultAsync(b => b.Id == model.Id);
            if (mevcut == null) return NotFound();

            var arac = await _vehicleRepo.GetirByIdAsync(model.VehicleId);
            if (arac != null) mevcut.Plaka = arac.Plaka;
            
            mevcut.VehicleId = model.VehicleId;
            mevcut.BakimTuru = model.BakimTuru;
            mevcut.BakimTarihi = model.BakimTarihi;
            mevcut.Maliyet = model.Maliyet;
            mevcut.Km = model.Km;
            mevcut.SonrakiBakimKm = model.SonrakiBakimKm;
            mevcut.SonrakiBakimTarihi = model.SonrakiBakimTarihi;
            mevcut.YapilanIslemler = model.YapilanIslemler;
            mevcut.ServisAdi = model.ServisAdi;

            await _db.SaveChangesAsync();
            await LogIslemAsync("Bakım Güncellendi", $"{mevcut.Plaka} aracı için bakım kaydı güncellendi.");
            TempData["Mesaj"] = "Bakım kaydı başarıyla güncellendi.";
            return RedirectToAction(nameof(Bakimlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BakimSil(int id)
        {
            var mevcut = await _db.AracBakimlari.FirstOrDefaultAsync(b => b.Id == id);
            if (mevcut != null)
            {
                var plaka = mevcut.Plaka;
                _db.AracBakimlari.Remove(mevcut);
                await _db.SaveChangesAsync();
                await LogIslemAsync("Bakım Silindi", $"{plaka} aracı için bakım kaydı silindi.");
                TempData["Mesaj"] = "Bakım kaydı silindi.";
            }
            return RedirectToAction(nameof(Bakimlar));
        }

        // ---------------- HGS VE CEZA SORGULAMA ----------------

        [HttpGet]
        public async Task<IActionResult> HgsBorc(string? plaka)
        {
            var araclar = await _vehicleRepo.TumuAsync();
            ViewBag.Araclar = araclar;
            ViewBag.SeciliPlaka = plaka;

            var model = await _hgsService.FiloHgsOzetiGetirAsync(araclar, plaka);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HgsGecisEkle(HgsGecis gecis)
        {
            if (ModelState.IsValid)
            {
                _db.HgsGecisleri.Add(gecis);
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = $"{gecis.Plaka} plakalı araç için HGS geçiş/ceza kaydı eklendi.";
            }
            else
            {
                TempData["Hata"] = "HGS geçiş kaydı eklenemedi. Lütfen tüm alanları doğru doldurunuz.";
            }
            return RedirectToAction(nameof(HgsBorc), new { plaka = gecis.Plaka });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HgsOde(int id, string plaka)
        {
            var item = await _db.HgsGecisleri.FirstOrDefaultAsync(h => h.Id == id);
            if (item != null)
            {
                item.OdediMi = true;
                await _db.SaveChangesAsync();
                TempData["Mesaj"] = "HGS borç kaydı ödendi olarak işaretlendi.";
            }
            return RedirectToAction(nameof(HgsBorc), new { plaka });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HgsSil(int id, string plaka)
        {
            var item = await _db.HgsGecisleri.FirstOrDefaultAsync(h => h.Id == id);
            if (item != null)
            {
                _db.HgsGecisleri.Remove(item);
                await _db.SaveChangesAsync();
                await LogIslemAsync("HGS Kaydı Silindi", $"{item.Plaka} plakalı araç için HGS kaydı silindi.");
                TempData["Mesaj"] = "HGS kaydı silindi.";
            }
            return RedirectToAction(nameof(HgsBorc), new { plaka });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HgsSorgula(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                TempData["Hata"] = "Lütfen sorgulamak için bir araç plakası seçiniz.";
                return RedirectToAction(nameof(HgsBorc));
            }

            var ozet = await _hgsService.BorcSorgulaAsync(plaka.Trim());

            if (!ozet.ApiKullanildi)
            {
                TempData["Hata"] = ozet.Mesaj;
            }
            else
            {
                TempData["Mesaj"] = ozet.Mesaj;
            }

            return RedirectToAction(nameof(HgsBorc), new { plaka = plaka.Trim() });
        }

        // ---------------- TEK BİRLEŞİK SİSTEM AYARLARI ----------------

        [HttpGet]
        public async Task<IActionResult> SistemKayitlari()
        {
            var logs = await _db.SystemLogs.OrderByDescending(l => l.Tarih).ToListAsync();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Ayarlar(string tab = "yoneticiler")
        {
            var me = await _adminRepo.GetirByIdAsync(MevcutKullaniciId);
            var smtp = _emailService.AyarlariGetir();
            var arvento = _arventoService.AyarlariGetir();
            ViewBag.Araclar = await _vehicleRepo.TumuAsync();

            var model = new SistemAyarlariPageViewModel
            {
                AktifTab = tab,
                Yoneticiler = await _adminRepo.TumuAsync(),
                YeniYoneticiModel = new YeniAdminViewModel(),
                SmtpModel = new SmtpAyarlarViewModel
                {
                    SmtpServer = smtp.SmtpServer,
                    Port = smtp.Port,
                    EnableSsl = smtp.EnableSsl,
                    SenderEmail = smtp.SenderEmail,
                    SenderPassword = "", // Şifre alanını güvenlik için her zaman boş gösteriyoruz
                    NotificationEmails = smtp.NotificationEmails ?? "",
                    Aktif = smtp.Aktif
                },
                ArventoModel = new ArventoAyarlarViewModel
                {
                    ApiUrl = arvento.ApiUrl,
                    KullaniciAdi = arvento.KullaniciAdi,
                    Sifre = "", // Şifre alanını güvenlik için her zaman boş gösteriyoruz
                    ApiKey = arvento.ApiKey,
                    Aktif = arvento.Aktif
                }
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profil()
        {
            var me = await _adminRepo.GetirByIdAsync(MevcutKullaniciId);
            var model = new ProfilViewModel
            {
                KullaniciAdi = me?.KullaniciAdi ?? "",
                AdSoyad = me?.AdSoyad ?? ""
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profil(ProfilViewModel model)
        {
            var admin = await _adminRepo.GetirByIdAsync(MevcutKullaniciId);
            if (admin != null)
            {
                if (!string.IsNullOrWhiteSpace(model.YeniSifre))
                {
                    if (string.IsNullOrWhiteSpace(model.MevcutSifre) || !PasswordHasher.Dogrula(model.MevcutSifre, admin.PasswordHash, admin.PasswordSalt))
                    {
                        TempData["Hata"] = "Mevcut şifreniz hatalı.";
                        return View(model);
                    }
                    var (hash, salt) = PasswordHasher.Hashle(model.YeniSifre);
                    admin.PasswordHash = hash;
                    admin.PasswordSalt = salt;
                }

                admin.AdSoyad = model.AdSoyad;
                await _adminRepo.GuncelleAsync(admin);
                await LogIslemAsync("Profil Güncellendi", "Yönetici kendi profil bilgilerini güncelledi.");
                TempData["Mesaj"] = "Profil bilgileriniz güncellendi.";
            }
            return RedirectToAction(nameof(Profil));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyarlarYoneticiEkle([Bind(Prefix = "YeniYoneticiModel")] YeniAdminViewModel model)
        {
            if (await _adminRepo.KullaniciAdiVarMiAsync(model.KullaniciAdi))
            {
                TempData["Hata"] = "Bu kullanıcı adı zaten kullanılıyor.";
                return RedirectToAction(nameof(Ayarlar), new { tab = "yoneticiler" });
            }

            var (hash, salt) = PasswordHasher.Hashle(model.Sifre);
            await _adminRepo.EkleAsync(new AdminUser
            {
                KullaniciAdi = model.KullaniciAdi,
                AdSoyad = model.AdSoyad,
                Rol = model.Rol,
                PasswordHash = hash,
                PasswordSalt = salt
            });

            await LogIslemAsync("Yönetici Eklendi", $"{model.KullaniciAdi} kullanıcı adıyla yeni yönetici eklendi.");
            TempData["Mesaj"] = "Yeni yönetici hesabı oluşturuldu.";
            return RedirectToAction(nameof(Ayarlar), new { tab = "yoneticiler" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyarlarYoneticiSil(int id)
        {
            var hedef = await _adminRepo.GetirByIdAsync(id);
            var mevcutId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (hedef != null && !hedef.AnaYonetici && hedef.Id.ToString() != mevcutId)
            {
                var kullAd = hedef.KullaniciAdi;
                await _adminRepo.SilAsync(id);
                await LogIslemAsync("Yönetici Silindi", $"{kullAd} kullanıcısı silindi.");
                TempData["Mesaj"] = "Yönetici hesabı silindi.";
            }
            else
            {
                TempData["Hata"] = "Ana yönetici veya kendi hesabınızı silemezsiniz.";
            }

            return RedirectToAction(nameof(Ayarlar), new { tab = "yoneticiler" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyarlarSmtp([Bind(Prefix = "SmtpModel")] SmtpAyarlarViewModel model, string? aksiyon)
        {
            var mevcutAyar = _emailService.AyarlariGetir();
            
            if (string.IsNullOrEmpty(model.SenderPassword))
            {
                model.SenderPassword = mevcutAyar.SenderPassword;
            }
            
            _emailService.AyarlariKaydet(model);

            if (aksiyon == "test")
            {
                var (basarili, mesaj) = await _emailService.TestEtAsync(model);
                TempData[basarili ? "Mesaj" : "Hata"] = mesaj;
            }
            else
            {
                TempData["Mesaj"] = "SMTP e-posta ayarları kaydedildi.";
            }

            return RedirectToAction(nameof(Ayarlar), new { tab = "smtp" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyarlarArvento([Bind(Prefix = "ArventoModel")] ArventoAyarlarViewModel model, string? aksiyon)
        {
            var mevcutAyar = _arventoService.AyarlariGetir();
            
            _arventoService.AyarlariKaydet(new ArventoAyari
            {
                ApiUrl = model.ApiUrl,
                KullaniciAdi = model.KullaniciAdi,
                Sifre = string.IsNullOrEmpty(model.Sifre) ? mevcutAyar.Sifre : model.Sifre,
                ApiKey = string.IsNullOrEmpty(model.ApiKey) ? mevcutAyar.ApiKey : model.ApiKey,
                Aktif = model.Aktif
            });

            if (aksiyon == "test")
            {
                var basarili = await _arventoService.BaglantiyiTestEtAsync();
                TempData[basarili ? "Mesaj" : "Hata"] = basarili
                    ? "✅ Arvento bağlantı testi başarılı!"
                    : "❌ Arvento bağlantı kurulamadı.";
            }
            else
            {
                TempData["Mesaj"] = "Arvento ayarları kaydedildi.";
            }

            return RedirectToAction(nameof(Ayarlar), new { tab = "arvento" });
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveMapData()
        {
            var data = await _arventoService.TumAracKonumlariAsync();
            if (data == null || data.Count == 0)
            {
                // Fallback to empty JSON if no real data or API is disabled
                return Json(new List<object>());
            }
            
            var result = data.Select(v => {
                string adresLower = (v.Adres ?? "").ToLowerInvariant();
                bool sirkette = adresLower.Contains("altınordu") || adresLower.Contains("altinordu") || 
                                adresLower.Contains("abdurrahman tatlıcı") || adresLower.Contains("abdurrahman tatlici");
                string aracDurumu = sirkette ? "Şirkette (İçeride)" : "Dışarıda";
                
                string kmBilgisi = v.ToplamKm.HasValue ? $" | {v.ToplamKm} KM" : "";
                return new {
                    lat = v.Enlem,
                    lng = v.Boylam,
                    plate = v.Plaka,
                    model = aracDurumu,
                    status = $"Hız: {v.Hiz} km/h{kmBilgisi} | {v.SonKonumZamani:HH:mm}",
                    driver = v.Adres ?? "Adres bilgisi yok"
                };
            });
            
            return Json(result);
        }
    }
}

