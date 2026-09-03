using AracGorevFormu.Models;

namespace AracGorevFormu.Models.ViewModels
{
    public class AracDetayViewModel
    {
        public Vehicle Arac { get; set; } = new();
        public List<GorevFormu> SonGorevler { get; set; } = new();
        public List<AracBakim> BakimGecmisi { get; set; } = new();
        public List<HgsGecis> HgsGecisleri { get; set; } = new();
    }
}
