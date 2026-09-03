using System.Collections.Generic;
using AracGorevFormu.Models;

namespace AracGorevFormu.Models.ViewModels
{
    public class MakineDashboardViewModel
    {
        public int ToplamMakineSayisi { get; set; }
        public int ToplamBakimSayisi { get; set; }
        public decimal ToplamBakimMaliyeti { get; set; }
        public int BuAykiBakimSayisi { get; set; }

        public List<MakineBakim> SonBakimlar { get; set; } = new List<MakineBakim>();
        
        // Lokasyon/Kategori bazlı makine sayıları
        public Dictionary<string, int> KategoriDagilimi { get; set; } = new Dictionary<string, int>();

        // En çok bakım yapılan / masraf çıkaran 5 makine (Ad ve Maliyet)
        public Dictionary<string, decimal> EnCokMaliyetliMakineler { get; set; } = new Dictionary<string, decimal>();
    }
}
