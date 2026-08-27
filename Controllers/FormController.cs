using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Mvc;

namespace AracGorevFormu.Controllers
{
    // Bu controller'daki tüm aksiyonlar KASITLI olarak [Authorize] İÇERMEZ.
    public class FormController : Controller
    {
        private readonly VehicleRepository _vehicleRepo;
        private readonly GorevFormuRepository _formRepo;
        private readonly IEmailService _emailService;

        public FormController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo, IEmailService emailService)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Yeni()
        {
            var model = new YeniGorevFormuViewModel
            {
                AktifAraclar = _vehicleRepo.AktifOlanlar()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yeni(YeniGorevFormuViewModel model)
        {
            var arac = _vehicleRepo.GetirById(model.VehicleId);
            if (arac == null || !arac.Aktif)
            {
                ModelState.AddModelError(nameof(model.VehicleId), "Seçilen araç bulunamadı veya artık aktif değil.");
            }

            if (model.PlanlananDonusZamani <= model.CikisZamani)
            {
                ModelState.AddModelError(nameof(model.PlanlananDonusZamani), "Planlanan dönüş zamanı, çıkış zamanından sonra olmalıdır.");
            }

            if (!ModelState.IsValid)
            {
                model.AktifAraclar = _vehicleRepo.AktifOlanlar();
                return View(model);
            }

            var form = new GorevFormu
            {
                VehicleId = arac!.Id,
                AracPlaka = arac.Plaka,
                AracMarka = arac.Marka,
                AracModel = arac.Model,
                KullananAdSoyad = model.KullananAdSoyad,
                KullananTelefon = model.KullananTelefon,
                Departman = model.Departman,
                GorevAmaci = model.GorevAmaci,
                CikisZamani = model.CikisZamani,
                PlanlananDonusZamani = model.PlanlananDonusZamani,
                Durum = GorevDurumu.Beklemede
            };

            _formRepo.Ekle(form);

            // E-posta bildirim gönderimi (varsa yapılandırılmış SMTP)
            await _emailService.FormBildirimiGonderAsync(form);

            return RedirectToAction("Basarili", new { takipKodu = form.TakipKodu });
        }

        [HttpGet]
        public IActionResult Basarili(string takipKodu)
        {
            var form = _formRepo.GetirByTakipKodu(takipKodu);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpGet]
        public IActionResult Sorgula()
        {
            return View(new SorgulaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sorgula(SorgulaViewModel model)
        {
            if (model.SorgulamaTipi == "kod")
            {
                // Takip kodu ile sorgulama
                if (string.IsNullOrWhiteSpace(model.TakipKodu))
                {
                    ModelState.AddModelError(nameof(model.TakipKodu), "Takip kodu zorunludur.");
                    return View(model);
                }

                var form = _formRepo.GetirByTakipKodu(model.TakipKodu.Trim());
                if (form == null)
                {
                    ModelState.AddModelError(nameof(model.TakipKodu), "Bu takip koduna ait bir görev formu bulunamadı.");
                    return View(model);
                }

                return RedirectToAction("Detay", new { takipKodu = form.TakipKodu });
            }
            else
            {
                // İsim-soyisim + telefon ile sorgulama
                if (string.IsNullOrWhiteSpace(model.AdSoyad))
                {
                    ModelState.AddModelError(nameof(model.AdSoyad), "Ad Soyad zorunludur.");
                    return View(model);
                }

                var sonuclar = _formRepo.AraByAdSoyadTelefon(model.AdSoyad, model.Telefon);

                if (sonuclar.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Bu bilgilere ait aktif bir görev formu bulunamadı.");
                    model.SonucGeldi = true;
                    model.SonucListesi = new List<GorevFormu>();
                    return View(model);
                }

                // Kullanıcının İSTEDİĞİ GİBİ: Eski geçmiş formlar yerine SADECE EN SON oluşturulmuş formu göster
                var enSonForm = sonuclar.OrderByDescending(f => f.OlusturmaTarihi).First();
                return RedirectToAction("Detay", new { takipKodu = enSonForm.TakipKodu });
            }
        }

        [HttpGet]
        public IActionResult Detay(string takipKodu)
        {
            var form = _formRepo.GetirByTakipKodu(takipKodu);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DonusBildir(string takipKodu)
        {
            var form = _formRepo.GetirByTakipKodu(takipKodu);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Onaylandi && form.GercekDonusZamani == null)
            {
                form.GercekDonusZamani = DateTime.Now;
                form.Durum = GorevDurumu.TamamlandiDondu;
                _formRepo.Guncelle(form);
                
                // Araç iade edildi (teslim edildi) e-postası gönder
                await _emailService.FormTamamlandiBildirimiGonderAsync(form);
                
                TempData["Mesaj"] = "Araç dönüşü başarıyla kaydedildi. İyi çalışmalar dileriz.";
            }

            return RedirectToAction("Detay", new { takipKodu });
        }
    }
}
