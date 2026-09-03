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

        public async Task<List<AdminUser>> TumuAsync() => await _db.AdminUsers.AsNoTracking().OrderBy(a => a.KullaniciAdi).ToListAsync();

        public async Task<AdminUser?> GetirByIdAsync(int id) => await _db.AdminUsers.FirstOrDefaultAsync(a => a.Id == id);

        public async Task<AdminUser?> GetirByKullaniciAdiAsync(string kullaniciAdi)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi)) return null;
            var normalized = kullaniciAdi.Trim().ToLower();
            return await _db.AdminUsers.FirstOrDefaultAsync(a => a.KullaniciAdi.ToLower() == normalized);
        }

        public async Task<bool> KullaniciAdiVarMiAsync(string kullaniciAdi) => await GetirByKullaniciAdiAsync(kullaniciAdi) != null;

        public async Task<AdminUser> EkleAsync(AdminUser admin)
        {
            admin.EklenmeTarihi = DateTime.Now;
            if (string.IsNullOrEmpty(admin.Rol)) admin.Rol = "Yönetici";
            _db.AdminUsers.Add(admin);
            await _db.SaveChangesAsync();
            return admin;
        }

        public async Task<bool> GuncelleAsync(AdminUser admin)
        {
            _db.AdminUsers.Update(admin);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> SilAsync(int id)
        {
            var target = await GetirByIdAsync(id);
            if (target == null) return false;
            _db.AdminUsers.Remove(target);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<int> AdminSayisiAsync() => await _db.AdminUsers.CountAsync();
    }
}
