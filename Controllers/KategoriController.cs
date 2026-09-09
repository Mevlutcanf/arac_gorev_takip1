using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class KategoriController : ControllerBase
    {
        private readonly AppDbContext _db;

        public KategoriController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{tur}")]
        public async Task<IActionResult> Listele(string tur)
        {
            var kategoriler = await _db.Kategoriler
                .Where(k => k.Tur == tur)
                .OrderBy(k => k.Ad)
                .Select(k => new { k.Id, k.Ad })
                .ToListAsync();
            
            return Ok(kategoriler);
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] KategoriEkleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Ad) || string.IsNullOrWhiteSpace(dto.Tur))
            {
                return BadRequest(new { Mesaj = "Kategori adı ve türü boş olamaz." });
            }

            // Aynı türde bu adla var mı kontrolü
            var mevcut = await _db.Kategoriler
                .FirstOrDefaultAsync(k => k.Tur == dto.Tur && k.Ad.ToLower() == dto.Ad.ToLower());

            if (mevcut != null)
            {
                return BadRequest(new { Mesaj = "Bu kategori zaten mevcut." });
            }

            var yeniKategori = new Kategori
            {
                Ad = dto.Ad.Trim(),
                Tur = dto.Tur
            };

            _db.Kategoriler.Add(yeniKategori);
            await _db.SaveChangesAsync();

            return Ok(new { Mesaj = "Başarılı", Id = yeniKategori.Id, Ad = yeniKategori.Ad });
        }
    }

    public class KategoriEkleDto
    {
        public string Ad { get; set; } = string.Empty;
        public string Tur { get; set; } = string.Empty;
    }
}
