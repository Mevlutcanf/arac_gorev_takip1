using System.Security.Claims;
using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IWebHostEnvironment _env;

        public AdminController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo,
            AdminUserRepository adminRepo, ArventoService arventoService, IEmailService emailService,
            IHgsService hgsService, AppDbContext db, IWebHostEnvironment env)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _adminRepo = adminRepo;
            _arventoService = arventoService;
            _emailService = emailService;
            _hgsService = hgsService;
            _db = db;
            _env = env;
        }

        private string MevcutKullaniciAdi => User.Identity?.Name ?? "Bilinmiyor";
        private int MevcutKullaniciId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private void LogIslem(string islemTuru, string detay)
        {
            var log = new SystemLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = MevcutKullaniciAdi,
                IslemTuru = islemTuru,
                Detay = detay
            };
            _db.SystemLogs.Add(log);
            _db.SaveChanges();
        }

        // ---------------- DASHBOARD ----------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tumFormlar = _formRepo.Tumu();
            var araclar = _vehicleRepo.Tumu();
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
        public IActionResult Formlar(string? durum)
        {
            var formlar = _formRepo.Tumu();

            if (!string.IsNullOrEmpty(durum) && Enum.TryParse<GorevDurumu>(durum, out var durumFiltre))
            {
                formlar = formlar.Where(f => f.Durum == durumFiltre).ToList();
            }

            ViewBag.SeciliDurum = durum;
            return View(formlar);
        }

        [HttpGet]
        public IActionResult FormDetay(int id)
        {
            var form = _formRepo.GetirById(id);
            if (form == null) return NotFound();
            return View(form);
        }

        /// <summary>
        /// Resmi Mevzuata Uyumlu Araç Görev ve Teslim Tutanağı (Baskı & Islak İmza Formatı)
        /// </summary>
        [HttpGet]
        public IActionResult ResmiTutanak(int id)
        {
            var form = _formRepo.GetirById(id);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormOnayla(int id)
        {
            var form = _formRepo.GetirById(id);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Beklemede)
            {
                form.Durum = GorevDurumu.Onaylandi;
                form.OnaylayanKullaniciAdi = MevcutKullaniciAdi;
                form.OnayTarihi = DateTime.Now;

                // Aracın anlık konumunu ve KM'sini çekip Çıkış KM'si olarak kaydet
                var arac = _vehicleRepo.GetirById(form.VehicleId);
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

                _formRepo.Guncelle(form);
                await _emailService.FormDurumDegisiklikBildirimiGonderAsync(form, onaylandi: true);
                LogIslem("Form Onaylandı", $"{form.AracPlaka} plakalı aracın görev formu onaylandı.");
                TempData["Mesaj"] = $"{form.AracPlaka} plakalı araç için görev formu onaylandı.";
            }

            return RedirectToAction(nameof(ResmiTutanak), new { id = form.Id });
        }

        [HttpGet]
        public IActionResult FormReddet(int id)
        {
            var form = _formRepo.GetirById(id);
            if (form == null) return NotFound();
            return View(new RedViewModel { FormId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormReddet(RedViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var form = _formRepo.GetirById(model.FormId);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Beklemede)
            {
                form.Durum = GorevDurumu.Reddedildi;
                form.OnaylayanKullaniciAdi = MevcutKullaniciAdi;
                form.OnayTarihi = DateTime.Now;
                form.RedNedeni = model.RedNedeni;
                _formRepo.Guncelle(form);
                await _emailService.FormDurumDegisiklikBildirimiGonderAsync(form, onaylandi: false);
                LogIslem("Form Reddedildi", $"{form.AracPlaka} plakalı aracın görev formu reddedildi. Neden: {model.RedNedeni}");
                TempData["Mesaj"] = "Görev formu reddedildi.";
            }

            return RedirectToAction(nameof(Formlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormDonusIsaretle(int id)
        {
            var form = _formRepo.GetirById(id);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Onaylandi && form.GercekDonusZamani == null)
            {
                form.GercekDonusZamani = DateTime.Now;
                form.Durum = GorevDurumu.TamamlandiDondu;

                // Aracın anlık konumunu ve KM'sini çekip Dönüş KM'si olarak kaydet
                var arac = _vehicleRepo.GetirById(form.VehicleId);
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

                _formRepo.Guncelle(form);
                
                // Araç iade edildi e-postası gönder
                await _emailService.FormTamamlandiBildirimiGonderAsync(form);
                
                LogIslem("Araç Döndü", $"{form.AracPlaka} plakalı aracın dönüşü (görevin tamamlanması) kaydedildi.");
                TempData["Mesaj"] = $"{form.AracPlaka} plakalı aracın dönüşü kaydedildi.";
            }

            return RedirectToAction(nameof(Formlar));
        }

        // ---------------- ARAÇ VE RUHSAT YÖNETİMİ ----------------

        [HttpGet]
        public IActionResult Araclar()
        {
            return View(_vehicleRepo.Tumu());
        }

        [HttpGet]
        public IActionResult AracEkle()
        {
            return View(new Vehicle { Aktif = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AracEkle(Vehicle vehicle)
        {
            if (_vehicleRepo.PlakaVarMi(vehicle.Plaka))
            {
                ModelState.AddModelError(nameof(vehicle.Plaka), "Bu plaka zaten kayıtlı.");
            }

            if (!ModelState.IsValid) return View(vehicle);

            _vehicleRepo.Ekle(vehicle);
            LogIslem("Araç Eklendi", $"{vehicle.Plaka} plakalı yeni araç filoya eklendi.");
            TempData["Mesaj"] = "Araç başarıyla eklendi.";
            return RedirectToAction(nameof(Araclar));
        }

        [HttpGet]
        public IActionResult AracDuzenle(int id)
        {
            var arac = _vehicleRepo.GetirById(id);
            if (arac == null) return NotFound();
            return View(arac);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracDuzenle(Vehicle vehicle, IFormFile? ruhsatDosya)
        {
            if (_vehicleRepo.PlakaVarMi(vehicle.Plaka, vehicle.Id))
            {
                ModelState.AddModelError(nameof(vehicle.Plaka), "Bu plaka başka bir araca kayıtlı.");
            }

            if (!ModelState.IsValid) return View(vehicle);

            // Ruhsat Dosyası Yükleme
            if (ruhsatDosya != null && ruhsatDosya.Length > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "ruhsatlar");
                Directory.CreateDirectory(uploadDir);

                var extension = Path.GetExtension(ruhsatDosya.FileName);
                var fileName = $"Ruhsat_{vehicle.Plaka.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ruhsatDosya.CopyToAsync(stream);
                }

                vehicle.RuhsatDosyaYolu = $"/uploads/ruhsatlar/{fileName}";
            }

            _vehicleRepo.Guncelle(vehicle);
            LogIslem("Araç Güncellendi", $"{vehicle.Plaka} plakalı aracın bilgileri güncellendi.");
            TempData["Mesaj"] = "Araç ve ruhsat bilgileri güncellendi.";
            return RedirectToAction(nameof(Araclar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AracSil(int id)
        {
            var kullanimda = _formRepo.Tumu().Any(f => f.VehicleId == id && f.AracDisarida);
            if (kullanimda)
            {
                TempData["Hata"] = "Bu araç şu anda kullanımda (dışarıda) olduğu için silinemez.";
                return RedirectToAction(nameof(Araclar));
            }

            var aracSilPlaka = _vehicleRepo.GetirById(id)?.Plaka ?? "Bilinmeyen";
            _vehicleRepo.Sil(id);
            LogIslem("Araç Silindi", $"{aracSilPlaka} plakalı araç silindi.");
            TempData["Mesaj"] = "Araç silindi.";
            return RedirectToAction(nameof(Araclar));
        }

        // ---------------- ARAÇ BAKIM VE SERVİS TAKİBİ ----------------

        [HttpGet]
        public IActionResult Bakimlar(int? vehicleId, DateTime? baslangicTarihi, DateTime? bitisTarihi)
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

            ViewBag.Araclar = _vehicleRepo.Tumu();
            ViewBag.SeciliVehicleId = vehicleId;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            
            return View(bakimlar.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BakimEkle(AracBakim bakim)
        {
            var arac = _vehicleRepo.GetirById(bakim.VehicleId);
            if (arac == null)
            {
                TempData["Hata"] = "Geçersiz araç seçim.";
                return RedirectToAction(nameof(Bakimlar));
            }

            bakim.Plaka = arac.Plaka;
            bakim.EklenmeTarihi = DateTime.Now;

            _db.AracBakimlari.Add(bakim);
            _db.SaveChanges();

            LogIslem("Bakım Eklendi", $"{arac.Plaka} plakalı araç için {bakim.BakimTuru} eklendi.");
            TempData["Mesaj"] = $"{arac.Plaka} plakalı araç için bakım kaydı eklendi.";
            return RedirectToAction(nameof(Bakimlar));
        }

        [HttpGet]
        public IActionResult BakimDuzenle(int id)
        {
            var bakim = _db.AracBakimlari.FirstOrDefault(b => b.Id == id);
            if (bakim == null) return NotFound();
            
            ViewBag.Araclar = _vehicleRepo.Tumu();
            return View(bakim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BakimDuzenle(AracBakim model)
        {
            var mevcut = _db.AracBakimlari.FirstOrDefault(b => b.Id == model.Id);
            if (mevcut == null) return NotFound();

            var arac = _vehicleRepo.GetirById(model.VehicleId);
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

            _db.SaveChanges();
            LogIslem("Bakım Güncellendi", $"{mevcut.Plaka} aracı için bakım kaydı güncellendi.");
            TempData["Mesaj"] = "Bakım kaydı başarıyla güncellendi.";
            return RedirectToAction(nameof(Bakimlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BakimSil(int id)
        {
            var mevcut = _db.AracBakimlari.FirstOrDefault(b => b.Id == id);
            if (mevcut != null)
            {
                var plaka = mevcut.Plaka;
                _db.AracBakimlari.Remove(mevcut);
                _db.SaveChanges();
                LogIslem("Bakım Silindi", $"{plaka} aracı için bakım kaydı silindi.");
                TempData["Mesaj"] = "Bakım kaydı silindi.";
            }
            return RedirectToAction(nameof(Bakimlar));
        }

        // ---------------- HGS VE CEZA SORGULAMA ----------------

        [HttpGet]
        public async Task<IActionResult> HgsBorc(string? plaka)
        {
            var araclar = _vehicleRepo.Tumu();
            ViewBag.Araclar = araclar;
            ViewBag.SeciliPlaka = plaka;

            var model = await _hgsService.FiloHgsOzetiGetirAsync(araclar, plaka);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HgsGecisEkle(HgsGecis gecis)
        {
            if (ModelState.IsValid)
            {
                _db.HgsGecisleri.Add(gecis);
                _db.SaveChanges();
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
        public IActionResult HgsOde(int id, string plaka)
        {
            var item = _db.HgsGecisleri.FirstOrDefault(h => h.Id == id);
            if (item != null)
            {
                item.OdediMi = true;
                _db.SaveChanges();
                TempData["Mesaj"] = "HGS borç kaydı ödendi olarak işaretlendi.";
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
        public IActionResult SistemKayitlari()
        {
            var logs = _db.SystemLogs.OrderByDescending(l => l.Tarih).ToList();
            return View(logs);
        }

        [HttpGet]
        public IActionResult Ayarlar(string tab = "yoneticiler")
        {
            var me = _adminRepo.GetirById(MevcutKullaniciId);
            var smtp = _emailService.AyarlariGetir();
            var arvento = _arventoService.AyarlariGetir();
            ViewBag.Araclar = _vehicleRepo.Tumu();

            var model = new SistemAyarlariPageViewModel
            {
                AktifTab = tab,
                Yoneticiler = _adminRepo.Tumu(),
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
        public IActionResult Profil()
        {
            var me = _adminRepo.GetirById(MevcutKullaniciId);
            var model = new ProfilViewModel
            {
                KullaniciAdi = me?.KullaniciAdi ?? "",
                AdSoyad = me?.AdSoyad ?? ""
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profil(ProfilViewModel model)
        {
            var admin = _adminRepo.GetirById(MevcutKullaniciId);
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
                _adminRepo.Guncelle(admin);
                LogIslem("Profil Güncellendi", "Yönetici kendi profil bilgilerini güncelledi.");
                TempData["Mesaj"] = "Profil bilgileriniz güncellendi.";
            }
            return RedirectToAction(nameof(Profil));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AyarlarYoneticiEkle([Bind(Prefix = "YeniYoneticiModel")] YeniAdminViewModel model)
        {
            if (_adminRepo.KullaniciAdiVarMi(model.KullaniciAdi))
            {
                TempData["Hata"] = "Bu kullanıcı adı zaten kullanılıyor.";
                return RedirectToAction(nameof(Ayarlar), new { tab = "yoneticiler" });
            }

            var (hash, salt) = PasswordHasher.Hashle(model.Sifre);
            _adminRepo.Ekle(new AdminUser
            {
                KullaniciAdi = model.KullaniciAdi,
                AdSoyad = model.AdSoyad,
                Rol = model.Rol,
                PasswordHash = hash,
                PasswordSalt = salt
            });

            LogIslem("Yönetici Eklendi", $"{model.KullaniciAdi} kullanıcı adıyla yeni yönetici eklendi.");
            TempData["Mesaj"] = "Yeni yönetici hesabı oluşturuldu.";
            return RedirectToAction(nameof(Ayarlar), new { tab = "yoneticiler" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AyarlarYoneticiSil(int id)
        {
            var hedef = _adminRepo.GetirById(id);
            var mevcutId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (hedef != null && !hedef.AnaYonetici && hedef.Id.ToString() != mevcutId)
            {
                var kullAd = hedef.KullaniciAdi;
                _adminRepo.Sil(id);
                LogIslem("Yönetici Silindi", $"{kullAd} kullanıcısı silindi.");
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

