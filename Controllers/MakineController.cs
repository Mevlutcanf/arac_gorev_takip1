using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracGorevFormu.Models.ViewModels;

namespace AracGorevFormu.Controllers
{
    [Authorize]
    public class MakineController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MakineController> _logger;

        public MakineController(AppDbContext context, ILogger<MakineController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string MevcutKullaniciAdi => User.Identity?.Name ?? "Bilinmiyor";

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
            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // GET: /Makine/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var model = new MakineDashboardViewModel();

            var makineler = await _context.Makineler.ToListAsync();
            var bakimlar = await _context.MakineBakimlari.Include(b => b.Makine).ToListAsync();

            model.ToplamMakineSayisi = makineler.Count;
            model.ToplamBakimSayisi = bakimlar.Count;
            model.ToplamBakimMaliyeti = bakimlar.Sum(b => b.Maliyet);
            model.BuAykiBakimSayisi = bakimlar.Count(b => b.BakimTarihi.Month == DateTime.Now.Month && b.BakimTarihi.Year == DateTime.Now.Year);

            // Son 5 bakım
            model.SonBakimlar = bakimlar.OrderByDescending(b => b.BakimTarihi).Take(5).ToList();

            // Lokasyon/Kategori dağılımı
            model.KategoriDagilimi = makineler
                .GroupBy(m => string.IsNullOrEmpty(m.Lokasyon) ? "Belirtilmedi" : m.Lokasyon)
                .ToDictionary(g => g.Key, g => g.Count());

            // En çok maliyet çıkaran 5 makine
            model.EnCokMaliyetliMakineler = bakimlar
                .GroupBy(b => b.Makine != null ? b.Makine.Ad : "Bilinmeyen Makine")
                .Select(g => new { MakineAd = g.Key, ToplamMaliyet = g.Sum(x => x.Maliyet) })
                .OrderByDescending(x => x.ToplamMaliyet)
                .Take(5)
                .ToDictionary(x => x.MakineAd, x => x.ToplamMaliyet);

            return View(model);
        }

        // GET: /Makine/Index
        public async Task<IActionResult> Index(string kategori)
        {
            var query = _context.Makineler.AsQueryable();

            if (!string.IsNullOrEmpty(kategori))
            {
                query = query.Where(m => m.Lokasyon == kategori);
            }

            var makineler = await query.OrderBy(m => m.Ad).ToListAsync();
            
            ViewBag.SeciliKategori = kategori;
            return View(makineler);
        }

        // GET: /Makine/Ekle
        public IActionResult Ekle()
        {
            return View(new Makine());
        }

        // POST: /Makine/Ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(Makine makine)
        {
            if (ModelState.IsValid)
            {
                makine.EklenmeTarihi = DateTime.Now;
                _context.Add(makine);
                await _context.SaveChangesAsync();
                
                await LogIslemAsync("Makine Eklendi", $"{makine.Ad} adlı makine sisteme eklendi.");
                
                TempData["SuccessMessage"] = "Makine başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(makine);
        }

        // GET: /Makine/Duzenle/5
        public async Task<IActionResult> Duzenle(int? id)
        {
            if (id == null) return NotFound();

            var makine = await _context.Makineler.FindAsync(id);
            if (makine == null) return NotFound();

            return View(makine);
        }

        // POST: /Makine/Duzenle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(int id, Makine makine)
        {
            if (id != makine.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(makine);
                    await _context.SaveChangesAsync();
                    
                    await LogIslemAsync("Makine Güncellendi", $"{makine.Ad} adlı makinenin bilgileri güncellendi.");
                    
                    TempData["SuccessMessage"] = "Makine başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await MakineExistsAsync(makine.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(makine);
        }

        // POST: /Makine/Sil/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var makine = await _context.Makineler.FindAsync(id);
            if (makine != null)
            {
                var makineAdi = makine.Ad;
                _context.Makineler.Remove(makine);
                await _context.SaveChangesAsync();
                
                await LogIslemAsync("Makine Silindi", $"{makineAdi} adlı makine sistemden silindi.");
                
                TempData["SuccessMessage"] = "Makine silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // BAKIM İŞLEMLERİ
        // ==========================================

        // GET: /Makine/TumBakimlar
        [HttpGet]
        public async Task<IActionResult> TumBakimlar(string? makineAdi, string? lokasyon, DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            var query = _context.MakineBakimlari
                                .Include(b => b.Makine)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(makineAdi))
                query = query.Where(b => b.Makine != null && b.Makine.Ad.Contains(makineAdi));

            if (!string.IsNullOrEmpty(lokasyon))
                query = query.Where(b => b.Makine != null && b.Makine.Lokasyon == lokasyon);

            if (baslangicTarihi.HasValue)
                query = query.Where(b => b.BakimTarihi >= baslangicTarihi.Value);

            if (bitisTarihi.HasValue)
                query = query.Where(b => b.BakimTarihi <= bitisTarihi.Value);

            var bakimlar = await query.OrderByDescending(b => b.BakimTarihi).ToListAsync();

            ViewBag.Lokasyonlar = await _context.Makineler.Select(m => m.Lokasyon).Where(l => l != null && l != "").Distinct().ToListAsync();
            ViewBag.MakineAdi = makineAdi;
            ViewBag.Lokasyon = lokasyon;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");

            return View(bakimlar);
        }

        // GET: /Makine/BakimListesi/5
        public async Task<IActionResult> BakimListesi(int makineId)
        {
            var makine = await _context.Makineler.FindAsync(makineId);
            if (makine == null) return NotFound();

            ViewBag.MakineAd = makine.Ad;
            ViewBag.MakineId = makine.Id;

            var bakimlar = await _context.MakineBakimlari
                .Where(b => b.MakineId == makineId)
                .OrderByDescending(b => b.BakimTarihi)
                .ToListAsync();

            return View(bakimlar);
        }

        // GET: /Makine/BakimEkle/5?
        public async Task<IActionResult> BakimEkle(int? makineId)
        {
            ViewBag.Makineler = await _context.Makineler.Where(m => m.Aktif).OrderBy(m => m.Ad).ToListAsync();
            
            var bakim = new MakineBakim
            {
                BakimTarihi = DateTime.Now
            };

            if (makineId.HasValue)
            {
                var makine = await _context.Makineler.FindAsync(makineId.Value);
                if (makine != null)
                {
                    ViewBag.MakineAd = makine.Ad;
                    bakim.MakineId = makineId.Value;
                    bakim.CalismaSaati = makine.CalismaSaati;
                }
            }

            return View(bakim);
        }

        // POST: /Makine/BakimEkle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BakimEkle(MakineBakim bakim, List<IFormFile> MakbuzDosyalar)
        {
            if (ModelState.IsValid)
            {
                bakim.EklenmeTarihi = DateTime.Now;
                _context.Add(bakim);

                // Makinenin çalışma saatini güncelle
                var makine = await _context.Makineler.FindAsync(bakim.MakineId);
                if (makine != null && bakim.CalismaSaati > makine.CalismaSaati)
                {
                    makine.CalismaSaati = bakim.CalismaSaati;
                    _context.Update(makine);
                }

                await _context.SaveChangesAsync();

                // Çoklu Dosya Yükleme İşlemi — Veritabanına kaydet (diske değil)
                if (MakbuzDosyalar != null && MakbuzDosyalar.Count > 0)
                {
                    var dosyaIdler = new List<string>();

                    foreach (var dosya in MakbuzDosyalar)
                    {
                        if (dosya.Length > 0)
                        {
                            using var ms = new MemoryStream();
                            await dosya.CopyToAsync(ms);

                            var dosyaEki = new DosyaEki
                            {
                                ParentTuru = "MakineBakimMakbuzu",
                                ParentId = bakim.Id,
                                DosyaAdi = dosya.FileName,
                                DosyaTipi = dosya.ContentType,
                                Icerik = ms.ToArray(),
                                YuklenmeTarihi = DateTime.Now
                            };
                            _context.DosyaEkleri.Add(dosyaEki);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // DosyaEki Id'lerini MakbuzDosyaYolu alanında sakla (geriye uyumluluk)
                    var eklenenDosyalar = _context.DosyaEkleri
                        .Where(d => d.ParentTuru == "MakineBakimMakbuzu" && d.ParentId == bakim.Id)
                        .Select(d => d.Id.ToString())
                        .ToList();

                    if (eklenenDosyalar.Any())
                    {
                        bakim.MakbuzDosyaYolu = string.Join(",", eklenenDosyalar.Select(id => $"db:{id}"));
                        await _context.SaveChangesAsync();
                    }
                }

                if (makine != null)
                {
                    await LogIslemAsync("Makine Bakımı Eklendi", $"{makine.Ad} makinesine yeni bakım/servis kaydı eklendi.");
                }

                TempData["SuccessMessage"] = "Makine bakım kaydı eklendi.";
                return RedirectToAction(nameof(BakimListesi), new { makineId = bakim.MakineId });
            }

            var makine2 = await _context.Makineler.FindAsync(bakim.MakineId);
            if (makine2 != null) ViewBag.MakineAd = makine2.Ad;
            
            ViewBag.Makineler = await _context.Makineler.Where(m => m.Aktif).OrderBy(m => m.Ad).ToListAsync();
            return View(bakim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BakimSil(int id)
        {
            var bakim = await _context.MakineBakimlari.FindAsync(id);
            if (bakim != null)
            {
                var makineId = bakim.MakineId;
                
                // Makbuz dosyalarını da sil
                var dosyalar = await _context.DosyaEkleri
                    .Where(d => d.ParentTuru == "MakineBakimMakbuzu" && d.ParentId == bakim.Id)
                    .ToListAsync();
                if (dosyalar.Any())
                {
                    _context.DosyaEkleri.RemoveRange(dosyalar);
                }

                _context.MakineBakimlari.Remove(bakim);
                await _context.SaveChangesAsync();
                await LogIslemAsync("Makine Bakımı Silindi", $"ID'si {id} olan makine bakım kaydı silindi.");
                TempData["SuccessMessage"] = "Makine bakım kaydı başarıyla silindi.";
                return RedirectToAction(nameof(BakimListesi), new { makineId = makineId });
            }
            return RedirectToAction(nameof(TumBakimlar));
        }

        /// <summary>
        /// Makbuz/fatura dosyasını veritabanından indirmek için endpoint
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MakbuzIndir(int id)
        {
            var dosya = await _context.DosyaEkleri.FirstOrDefaultAsync(d => d.Id == id);
            if (dosya == null || dosya.Icerik == null || dosya.Icerik.Length == 0)
                return NotFound();

            return File(dosya.Icerik, dosya.DosyaTipi ?? "application/octet-stream", dosya.DosyaAdi ?? $"Makbuz_{id}.dat");
        }


        private async Task<bool> MakineExistsAsync(int id)
        {
            return await _context.Makineler.AnyAsync(e => e.Id == id);
        }
    }
}
