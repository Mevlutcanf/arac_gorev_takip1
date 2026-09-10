using AracGorevFormu.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.ViewComponents
{
    public class ChangelogViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public ChangelogViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var guncellemeler = await _db.SistemGuncellemeleri
                .OrderByDescending(g => g.EklenmeTarihi)
                .Take(5) // Son 5 güncellemeyi al
                .ToListAsync();

            return View(guncellemeler);
        }
    }
}
