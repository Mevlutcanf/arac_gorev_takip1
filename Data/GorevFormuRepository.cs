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

        public async Task<List<GorevFormu>> TumuAsync() => await _db.GorevFormlari.AsNoTracking().OrderByDescending(f => f.OlusturmaTarihi).ToListAsync();

        public async Task<GorevFormu?> GetirByIdAsync(int id) => await _db.GorevFormlari.FirstOrDefaultAsync(f => f.Id == id);

        public async Task<GorevFormu?> GetirByTakipKoduAsync(string takipKodu)
        {
            if (string.IsNullOrWhiteSpace(takipKodu)) return null;
            var normalized = takipKodu.Trim().ToLower();
            return await _db.GorevFormlari.FirstOrDefaultAsync(f => f.TakipKodu.ToLower() == normalized);
        }

        /// <summary>
        /// İsim-soyisim ve opsiyonel telefon numarası ile arama yapıp en güncel kayıtları döndürür.
        /// </summary>
        public async Task<List<GorevFormu>> AraByAdSoyadTelefonAsync(string adSoyad, string? telefon)
        {
            var normalizedAd = adSoyad.Trim().ToLower();

            var query = _db.GorevFormlari.AsNoTracking()
                .Where(f => f.KullananAdSoyad.ToLower().Contains(normalizedAd));

            if (!string.IsNullOrWhiteSpace(telefon))
            {
                var normalizedTel = telefon.Trim().Replace(" ", "").Replace("-", "");
                query = query.Where(f => f.KullananTelefon.Replace(" ", "").Replace("-", "").Contains(normalizedTel));
            }

            return await query.OrderByDescending(f => f.OlusturmaTarihi).ToListAsync();
        }

        public async Task<List<GorevFormu>> DisaridaOlanlarAsync() =>
            await _db.GorevFormlari.AsNoTracking().Where(f => f.Durum == GorevDurumu.Onaylandi && f.GercekDonusZamani == null)
                .OrderByDescending(f => f.OlusturmaTarihi).ToListAsync();

        public async Task<List<GorevFormu>> BeklemedeOlanlarAsync() =>
            await _db.GorevFormlari.AsNoTracking().Where(f => f.Durum == GorevDurumu.Beklemede)
                .OrderByDescending(f => f.OlusturmaTarihi).ToListAsync();

        public async Task<GorevFormu> EkleAsync(GorevFormu form)
        {
            form.OlusturmaTarihi = DateTime.Now;
            form.TakipKodu = await TakipKoduUretAsync();
            _db.GorevFormlari.Add(form);
            await _db.SaveChangesAsync();
            return form;
        }

        public async Task<bool> GuncelleAsync(GorevFormu form)
        {
            _db.GorevFormlari.Update(form);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> SilAsync(int id)
        {
            var form = await _db.GorevFormlari.FindAsync(id);
            if (form != null)
            {
                _db.GorevFormlari.Remove(form);
                return await _db.SaveChangesAsync() > 0;
            }
            return false;
        }

        private async Task<string> TakipKoduUretAsync()
        {
            var rnd = new Random();
            string kod;
            do
            {
                kod = "AT-" + rnd.Next(100000, 999999);
            } while (await _db.GorevFormlari.AnyAsync(f => f.TakipKodu == kod));
            return kod;
        }
    }
}
