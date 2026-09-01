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

        public List<Vehicle> Tumu() => _db.Vehicles.AsNoTracking().OrderBy(v => v.Plaka).ToList();

        public List<Vehicle> AktifOlanlar() => Tumu().Where(v => v.Aktif).ToList();

        public Vehicle? GetirById(int id) => _db.Vehicles.FirstOrDefault(v => v.Id == id);

        public bool PlakaVarMi(string plaka, int? haricTutulanId = null)
        {
            var normalized = plaka.Trim().ToLower().Replace(" ", "");
            return _db.Vehicles.Any(v =>
                v.Plaka.ToLower().Replace(" ", "") == normalized &&
                (haricTutulanId == null || v.Id != haricTutulanId));
        }

        public Vehicle Ekle(Vehicle vehicle)
        {
            vehicle.EklenmeTarihi = DateTime.Now;
            _db.Vehicles.Add(vehicle);
            _db.SaveChanges();
            return vehicle;
        }

        public bool Guncelle(Vehicle vehicle)
        {
            _db.Vehicles.Update(vehicle);
            return _db.SaveChanges() > 0;
        }

        public bool Sil(int id)
        {
            var target = GetirById(id);
            if (target == null) return false;
            _db.Vehicles.Remove(target);
            return _db.SaveChanges() > 0;
        }
    }
}
