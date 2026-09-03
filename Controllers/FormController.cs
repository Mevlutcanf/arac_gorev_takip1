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
        private readonly IServiceScopeFactory _scopeFactory;

        public FormController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo, IEmailService emailService, IServiceScopeFactory scopeFactory)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Yeni()
        {
            var aktifAraclar = await _vehicleRepo.AktifOlanlarAsync();
            var disaridakiFormlar = await _formRepo.DisaridaOlanlarAsync();
            var beklemedekiFormlar = await _formRepo.BeklemedeOlanlarAsync();

            var model = new YeniGorevFormuViewModel
            {
                AktifAraclar = aktifAraclar
            };
            ViewBag.DisaridakiAracIdleri = disaridakiFormlar.Select(f => f.VehicleId).ToList();
            ViewBag.OnayBekleyenAracIdleri = beklemedekiFormlar.Select(f => f.VehicleId).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yeni(YeniGorevFormuViewModel model)
        {
            var arac = await _vehicleRepo.GetirByIdAsync(model.VehicleId);
            if (arac == null || !arac.Aktif)
            {
                ModelState.AddModelError(nameof(model.VehicleId), "Seçilen araç bulunamadı veya artık aktif değil.");
            }
            else
            {
                var beklemedekiFormlar = await _formRepo.BeklemedeOlanlarAsync();
                var disaridakiFormlar = await _formRepo.DisaridaOlanlarAsync();
                
                var musaitDegil = beklemedekiFormlar.Any(f => f.VehicleId == arac.Id) || disaridakiFormlar.Any(f => f.VehicleId == arac.Id);
                if (musaitDegil)
                {
                    ModelState.AddModelError(nameof(model.VehicleId), "Bu araç şu anda görevde veya onay bekleyen bir görevi var. Lütfen 'Müsait' durumda olan başka bir araç seçiniz.");
                }
            }


            if (!ModelState.IsValid)
            {
                model.AktifAraclar = await _vehicleRepo.AktifOlanlarAsync();
                var disaridakiFormlar = await _formRepo.DisaridaOlanlarAsync();
                var beklemedekiFormlar = await _formRepo.BeklemedeOlanlarAsync();
                ViewBag.DisaridakiAracIdleri = disaridakiFormlar.Select(f => f.VehicleId).ToList();
                ViewBag.OnayBekleyenAracIdleri = beklemedekiFormlar.Select(f => f.VehicleId).ToList();
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
                Durum = GorevDurumu.Beklemede
            };

            await _formRepo.EkleAsync(form);

            // Kullanıcı görsel bir geçiş/yükleme ekranı görmek istediği için e-posta gönderimi tamamlanana kadar (senkron) bekliyoruz
            await _emailService.FormBildirimiGonderAsync(form);

            return RedirectToAction("Basarili", new { takipKodu = form.TakipKodu });
        }

        [HttpGet]
        public async Task<IActionResult> Basarili(string takipKodu)
        {
            var form = await _formRepo.GetirByTakipKoduAsync(takipKodu);
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
        public async Task<IActionResult> Sorgula(SorgulaViewModel model)
        {
            if (model.SorgulamaTipi == "kod")
            {
                // Takip kodu ile sorgulama
                if (string.IsNullOrWhiteSpace(model.TakipKodu))
                {
                    ModelState.AddModelError(nameof(model.TakipKodu), "Takip kodu zorunludur.");
                    return View(model);
                }

                var form = await _formRepo.GetirByTakipKoduAsync(model.TakipKodu.Trim());
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

                var sonuclar = await _formRepo.AraByAdSoyadTelefonAsync(model.AdSoyad, model.Telefon);

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
        public async Task<IActionResult> Detay(string takipKodu)
        {
            var form = await _formRepo.GetirByTakipKoduAsync(takipKodu);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DonusBildir(string takipKodu)
        {
            var form = await _formRepo.GetirByTakipKoduAsync(takipKodu);
            if (form == null) return NotFound();

            if (form.Durum == GorevDurumu.Onaylandi && form.GercekDonusZamani == null)
            {
                form.GercekDonusZamani = DateTime.Now;
                form.Durum = GorevDurumu.TamamlandiDondu;
                await _formRepo.GuncelleAsync(form);
                
                // Araç iade edildi (teslim edildi) e-postası gönder
                await _emailService.FormTamamlandiBildirimiGonderAsync(form);
                
                TempData["Mesaj"] = "Araç dönüşü başarıyla kaydedildi. İyi çalışmalar dileriz.";
            }

            return RedirectToAction("Detay", new { takipKodu });
        }
    }
}
