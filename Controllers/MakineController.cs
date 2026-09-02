using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // GET: /Makine
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
                    TempData["SuccessMessage"] = "Makine başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MakineExists(makine.Id)) return NotFound();
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
                _context.Makineler.Remove(makine);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Makine silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // BAKIM İŞLEMLERİ
        // ==========================================

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
        public async Task<IActionResult> BakimEkle(MakineBakim bakim, IFormFile? MakbuzDosya)
        {
            if (ModelState.IsValid)
            {
                // Dosya Yükleme ve OCR İşlemi
                if (MakbuzDosya != null && MakbuzDosya.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "makbuzlar");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + MakbuzDosya.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await MakbuzDosya.CopyToAsync(fileStream);
                    }
                    
                    bakim.MakbuzDosyaYolu = "/uploads/makbuzlar/" + uniqueFileName;
                }

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
                TempData["SuccessMessage"] = "Makine bakım kaydı eklendi.";
                return RedirectToAction(nameof(BakimListesi), new { makineId = bakim.MakineId });
            }

            var makine2 = await _context.Makineler.FindAsync(bakim.MakineId);
            if (makine2 != null) ViewBag.MakineAd = makine2.Ad;
            
            ViewBag.Makineler = await _context.Makineler.Where(m => m.Aktif).OrderBy(m => m.Ad).ToListAsync();
            return View(bakim);
        }



        private bool MakineExists(int id)
        {
            return _context.Makineler.Any(e => e.Id == id);
        }
    }
}
