using AracGorevFormu.Models;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Data
{
    public class AdminUserRepository
    {
        private readonly AppDbContext _db;

        public AdminUserRepository(AppDbContext db)
        {
            _db = db;
        }

        public List<AdminUser> Tumu() => _db.AdminUsers.AsNoTracking().OrderBy(a => a.KullaniciAdi).ToList();

        public AdminUser? GetirById(int id) => _db.AdminUsers.FirstOrDefault(a => a.Id == id);

        public AdminUser? GetirByKullaniciAdi(string kullaniciAdi)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi)) return null;
            var normalized = kullaniciAdi.Trim().ToLower();
            return _db.AdminUsers.FirstOrDefault(a => a.KullaniciAdi.ToLower() == normalized);
        }

        public bool KullaniciAdiVarMi(string kullaniciAdi) => GetirByKullaniciAdi(kullaniciAdi) != null;

        public AdminUser Ekle(AdminUser admin)
        {
            admin.EklenmeTarihi = DateTime.Now;
            if (string.IsNullOrEmpty(admin.Rol)) admin.Rol = "Yönetici";
            _db.AdminUsers.Add(admin);
            _db.SaveChanges();
            return admin;
        }

        public bool Guncelle(AdminUser admin)
        {
            _db.AdminUsers.Update(admin);
            return _db.SaveChanges() > 0;
        }

        public bool Sil(int id)
        {
            var target = GetirById(id);
            if (target == null) return false;
            _db.AdminUsers.Remove(target);
            return _db.SaveChanges() > 0;
        }

        public int AdminSayisi() => _db.AdminUsers.Count();
    }
}
