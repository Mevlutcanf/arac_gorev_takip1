using AracGorevFormu.Models;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Data
{
    public class GorevFormuRepository
    {
        private readonly AppDbContext _db;

        public GorevFormuRepository(AppDbContext db)
        {
            _db = db;
        }

        public List<GorevFormu> Tumu() => _db.GorevFormlari.OrderByDescending(f => f.OlusturmaTarihi).ToList();

        public GorevFormu? GetirById(int id) => _db.GorevFormlari.FirstOrDefault(f => f.Id == id);

        public GorevFormu? GetirByTakipKodu(string takipKodu)
        {
            if (string.IsNullOrWhiteSpace(takipKodu)) return null;
            var normalized = takipKodu.Trim().ToLower();
            return _db.GorevFormlari.FirstOrDefault(f => f.TakipKodu.ToLower() == normalized);
        }

        /// <summary>
        /// İsim-soyisim ve opsiyonel telefon numarası ile arama yapıp en güncel kayıtları döndürür.
        /// </summary>
        public List<GorevFormu> AraByAdSoyadTelefon(string adSoyad, string? telefon)
        {
            var normalizedAd = adSoyad.Trim().ToLower();

            var query = _db.GorevFormlari.AsQueryable()
                .Where(f => f.KullananAdSoyad.ToLower().Contains(normalizedAd));

            if (!string.IsNullOrWhiteSpace(telefon))
            {
                var normalizedTel = telefon.Trim().Replace(" ", "").Replace("-", "");
                query = query.Where(f => f.KullananTelefon.Replace(" ", "").Replace("-", "").Contains(normalizedTel));
            }

            return query.OrderByDescending(f => f.OlusturmaTarihi).ToList();
        }

        public List<GorevFormu> DisaridaOlanlar() =>
            _db.GorevFormlari.Where(f => f.Durum == GorevDurumu.Onaylandi && f.GercekDonusZamani == null)
                .OrderByDescending(f => f.OlusturmaTarihi).ToList();

        public List<GorevFormu> BeklemedeOlanlar() =>
            _db.GorevFormlari.Where(f => f.Durum == GorevDurumu.Beklemede)
                .OrderByDescending(f => f.OlusturmaTarihi).ToList();

        public GorevFormu Ekle(GorevFormu form)
        {
            form.OlusturmaTarihi = DateTime.Now;
            form.TakipKodu = TakipKoduUret();
            _db.GorevFormlari.Add(form);
            _db.SaveChanges();
            return form;
        }

        public bool Guncelle(GorevFormu form)
        {
            _db.GorevFormlari.Update(form);
            return _db.SaveChanges() > 0;
        }

        private string TakipKoduUret()
        {
            var rnd = new Random();
            string kod;
            do
            {
                kod = "GF-" + rnd.Next(100000, 999999);
            } while (_db.GorevFormlari.Any(f => f.TakipKodu == kod));
            return kod;
        }
    }
}
