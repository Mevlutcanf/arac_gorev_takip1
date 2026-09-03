using AracGorevFormu.Models;

namespace AracGorevFormu.Models.ViewModels
{
    public class MakineDetayViewModel
    {
        public Makine Makine { get; set; } = new();
        public List<MakineBakim> BakimGecmisi { get; set; } = new();
    }
}
