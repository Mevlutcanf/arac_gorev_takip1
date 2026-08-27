using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class AracBakim
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Araç Plakası")]
        public string Plaka { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bakım Tarihi")]
        public DateTime BakimTarihi { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [Display(Name = "Bakım / İşlem Türü")]
        public string BakimTuru { get; set; } = "Periyodik Bakım"; // Periyodik Bakım, Yağ Değişimi, Muayene, Lastik, Tamir

        [Display(Name = "Bakım Yapıldığı Anki Km")]
        public int Km { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Yapılan İşlemler ve Değişen Parçalar")]
        public string YapilanIslemler { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Servis / Tamirhane Adı")]
        public string? ServisAdi { get; set; }

        [Display(Name = "Toplam Maliyet (TL)")]
        public decimal Maliyet { get; set; }

        [Display(Name = "Sonraki Bakım Tarihi")]
        public DateTime? SonrakiBakimTarihi { get; set; }

        [Display(Name = "Sonraki Bakım Km")]
        public int? SonrakiBakimKm { get; set; }

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }

    public class HgsGecis
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Display(Name = "Geçiş / Ceza Tarihi")]
        public DateTime GecisTarihi { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "Geçiş Noktası / Otoyol / Gişe")]
        public string GiseAdı { get; set; } = string.Empty;

        [Display(Name = "Tutar (TL)")]
        public decimal Tutar { get; set; }

        [Display(Name = "Ödenme Durumu")]
        public bool OdediMi { get; set; } = false;

        [Display(Name = "İhlalli Geçiş / İdari Ceza Mı?")]
        public bool CezaMi { get; set; } = false;
    }
}
