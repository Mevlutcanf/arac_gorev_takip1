using AracGorevFormu.Models;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Data
{
    public class VehicleRepository
    {
        private readonly AppDbContext _db;

        public VehicleRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Vehicle>> TumuAsync()
        {
            return await _db.Vehicles
                .AsNoTracking()
                .OrderBy(v => v.Plaka)
                .Select(v => new Vehicle
                {
                    Id = v.Id,
                    Plaka = v.Plaka,
                    Marka = v.Marka,
                    Model = v.Model,
                    Renk = v.Renk,
                    SahiplikTuru = v.SahiplikTuru,
                    SabitSurucu = v.SabitSurucu,
                    Lokasyon = v.Lokasyon,
                    Aktif = v.Aktif,
                    EklenmeTarihi = v.EklenmeTarihi,
                    SasiNo = v.SasiNo,
                    MotorNo = v.MotorNo,
                    TescilTarihi = v.TescilTarihi,
                    MuayeneBitisTarihi = v.MuayeneBitisTarihi,
                    SigortaBitisTarihi = v.SigortaBitisTarihi,
                    RuhsatDosyaYolu = v.RuhsatDosyaYolu,
                    RuhsatDosyaAdi = v.RuhsatDosyaAdi,
                    RuhsatDosyaTipi = v.RuhsatDosyaTipi,
                    // RuhsatDosyaIcerigi KASITLI OLARAK HARİÇ TUTULDU (RAM SIZINTISINI ÖNLEMEK İÇİN)
                    GuncelKm = v.GuncelKm,
                    SonKonumZamani = v.SonKonumZamani,
                    SonAdres = v.SonAdres
                })
                .ToListAsync();
        }

        public async Task<List<Vehicle>> AktifOlanlarAsync()
        {
            var tumu = await TumuAsync();
            return tumu.Where(v => v.Aktif).ToList();
        }

        public async Task<Vehicle?> GetirByIdAsync(int id)
        {
            // Tekil detayda dosya içeriği lazım olabilir, o yüzden Select KULLANILMAZ.
            return await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> PlakaVarMiAsync(string plaka, int? haricTutulanId = null)
        {
            var normalized = plaka.Trim().ToLower().Replace(" ", "");
            return await _db.Vehicles.AnyAsync(v =>
                v.Plaka.ToLower().Replace(" ", "") == normalized &&
                (haricTutulanId == null || v.Id != haricTutulanId));
        }

        public async Task<Vehicle> EkleAsync(Vehicle vehicle)
        {
            vehicle.EklenmeTarihi = DateTime.Now;
            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();
            return vehicle;
        }

        public async Task<bool> GuncelleAsync(Vehicle vehicle)
        {
            _db.Vehicles.Update(vehicle);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> SilAsync(int id)
        {
            var target = await GetirByIdAsync(id);
            if (target == null) return false;
            _db.Vehicles.Remove(target);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
