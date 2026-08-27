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

        // ---------------- DASHBOARD ----------------

        [HttpGet]
        public IActionResult Index()
        {
            var tumFormlar = _formRepo.Tumu();
            var araclar = _vehicleRepo.Tumu();
            var disaridakiFormlar = tumFormlar.Where(f => f.AracDisarida).ToList();
            var disaridakiAracIdleri = disaridakiFormlar.Select(f => f.VehicleId).ToHashSet();

            var model = new AdminDashboardViewModel
            {
                ToplamArac = araclar.Count,
                AktifArac = araclar.Count(a => a.Aktif),
                SuAndaDisaridaOlan = disaridakiFormlar.Count,
                SuAndaIcerideOlan = araclar.Count(a => a.Aktif && !disaridakiAracIdleri.Contains(a.Id)),
                BekleyenOnaySayisi = tumFormlar.Count(f => f.Durum == GorevDurumu.Beklemede),
                BugunCikanSayisi = tumFormlar.Count(f => f.OlusturmaTarihi.Date == DateTime.Today),
                SonFormlar = tumFormlar.Take(10).ToList(),
                DisaridakiAraclar = disaridakiFormlar,
                IceridekiAraclar = araclar.Where(a => a.Aktif && !disaridakiAracIdleri.Contains(a.Id)).ToList()
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
                _formRepo.Guncelle(form);
                await _emailService.FormDurumDegisiklikBildirimiGonderAsync(form, onaylandi: true);
                TempData["Mesaj"] = $"{form.AracPlaka} plakalı araç için görev formu onaylandı.";
            }

            return RedirectToAction(nameof(Formlar));
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
                _formRepo.Guncelle(form);
                
                // Araç iade edildi e-postası gönder
                await _emailService.FormTamamlandiBildirimiGonderAsync(form);
                
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

            _vehicleRepo.Sil(id);
            TempData["Mesaj"] = "Araç silindi.";
            return RedirectToAction(nameof(Araclar));
        }

        // ---------------- ARAÇ BAKIM VE SERVİS TAKİBİ ----------------

        [HttpGet]
        public IActionResult Bakimlar(int? vehicleId)
        {
            var bakimlar = _db.AracBakimlari.OrderByDescending(b => b.BakimTarihi).ToList();
            if (vehicleId != null)
            {
                bakimlar = bakimlar.Where(b => b.VehicleId == vehicleId).ToList();
            }

            ViewBag.Araclar = _vehicleRepo.Tumu();
            ViewBag.SeciliVehicleId = vehicleId;
            return View(bakimlar);
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
                _db.AracBakimlari.Remove(mevcut);
                _db.SaveChanges();
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
        public IActionResult Ayarlar(string tab = "yoneticiler")
        {
            var me = _adminRepo.GetirById(MevcutKullaniciId);
            var smtp = _emailService.AyarlariGetir();
            var arvento = _arventoService.AyarlariGetir();
            ViewBag.Araclar = _vehicleRepo.Tumu();

            var model = new SistemAyarlariPageViewModel
            {
                AktifTab = tab,
                ProfilModel = new ProfilViewModel
                {
                    KullaniciAdi = me?.KullaniciAdi ?? "",
                    AdSoyad = me?.AdSoyad ?? ""
                },
                Yoneticiler = _adminRepo.Tumu(),
                YeniYoneticiModel = new YeniAdminViewModel(),
                SmtpModel = new SmtpAyarlarViewModel
                {
                    SmtpServer = smtp.SmtpServer,
                    Port = smtp.Port,
                    EnableSsl = smtp.EnableSsl,
                    SenderEmail = smtp.SenderEmail,
                    SenderPassword = string.IsNullOrWhiteSpace(smtp.SenderPassword) ? "" : "••••••••",
                    NotificationEmails = smtp.NotificationEmails ?? "",
                    Aktif = smtp.Aktif
                },
                ArventoModel = new ArventoAyarlarViewModel
                {
                    ApiUrl = arvento.ApiUrl,
                    KullaniciAdi = arvento.KullaniciAdi,
                    Sifre = arvento.Sifre,
                    ApiKey = arvento.ApiKey,
                    Aktif = arvento.Aktif
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AyarlarProfil([Bind(Prefix = "ProfilModel")] ProfilViewModel model)
        {
            var admin = _adminRepo.GetirById(MevcutKullaniciId);
            if (admin != null)
            {
                if (!string.IsNullOrWhiteSpace(model.YeniSifre))
                {
                    if (string.IsNullOrWhiteSpace(model.MevcutSifre) || !PasswordHasher.Dogrula(model.MevcutSifre, admin.PasswordHash, admin.PasswordSalt))
                    {
                        TempData["Hata"] = "Mevcut şifreniz hatalı.";
                        return RedirectToAction(nameof(Ayarlar), new { tab = "profil" });
                    }
                    var (hash, salt) = PasswordHasher.Hashle(model.YeniSifre);
                    admin.PasswordHash = hash;
                    admin.PasswordSalt = salt;
                }

                admin.AdSoyad = model.AdSoyad;
                _adminRepo.Guncelle(admin);
                TempData["Mesaj"] = "Profil bilgileriniz güncellendi.";
            }

            return RedirectToAction(nameof(Ayarlar), new { tab = "profil" });
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
                _adminRepo.Sil(id);
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
            _arventoService.AyarlariKaydet(new ArventoAyari
            {
                ApiUrl = model.ApiUrl,
                KullaniciAdi = model.KullaniciAdi,
                Sifre = model.Sifre,
                ApiKey = model.ApiKey,
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
    }
}
