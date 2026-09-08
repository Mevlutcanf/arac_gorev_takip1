using AracGorevFormu.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AracGorevFormu.ViewComponents
{
    public class VadeYaklasanAraclarViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public VadeYaklasanAraclarViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var bugun = DateTime.Today;
            var otuzGunSonrasi = bugun.AddDays(30);

            var yaklasanMuayeneler = await _context.Vehicles
                .Where(v => v.Aktif && v.MuayeneBitisTarihi.HasValue && v.MuayeneBitisTarihi.Value.Date <= otuzGunSonrasi && v.MuayeneBitisTarihi.Value.Date >= bugun.AddDays(-365))
                .Select(v => new VadeBildirimModel
                {
                    Plaka = v.Plaka,
                    AracIsmi = $"{v.Marka} {v.Model}",
                    VadeTipi = "Muayene",
                    BitisTarihi = v.MuayeneBitisTarihi.Value,
                    KalanGun = (v.MuayeneBitisTarihi.Value.Date - bugun).Days
                })
                .ToListAsync();

            var yaklasanSigortalar = await _context.Vehicles
                .Where(v => v.Aktif && v.SigortaBitisTarihi.HasValue && v.SigortaBitisTarihi.Value.Date <= otuzGunSonrasi && v.SigortaBitisTarihi.Value.Date >= bugun.AddDays(-365))
                .Select(v => new VadeBildirimModel
                {
                    Plaka = v.Plaka,
                    AracIsmi = $"{v.Marka} {v.Model}",
                    VadeTipi = "Sigorta/Kasko",
                    BitisTarihi = v.SigortaBitisTarihi.Value,
                    KalanGun = (v.SigortaBitisTarihi.Value.Date - bugun).Days
                })
                .ToListAsync();

            var tumBildirimler = yaklasanMuayeneler.Concat(yaklasanSigortalar)
                                .OrderBy(x => x.KalanGun)
                                .ToList();

            return View(tumBildirimler);
        }
    }

    public class VadeBildirimModel
    {
        public string Plaka { get; set; } = string.Empty;
        public string AracIsmi { get; set; } = string.Empty;
        public string VadeTipi { get; set; } = string.Empty;
        public DateTime BitisTarihi { get; set; }
        public int KalanGun { get; set; }
    }
}
