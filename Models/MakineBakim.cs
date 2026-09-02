using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AracGorevFormu.Models
{
    public class MakineBakim
    {
        public int Id { get; set; }

        public int MakineId { get; set; }

        [ForeignKey("MakineId")]
        public Makine? Makine { get; set; }

        [Required]
        [Display(Name = "Bakım Tarihi")]
        public DateTime BakimTarihi { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [Display(Name = "Bakım / İşlem Türü")]
        public string BakimTuru { get; set; } = "Periyodik Bakım"; // Periyodik Bakım, Arıza/Tamir, Kurulum, Kalibrasyon

        [Display(Name = "Bakım Yapıldığı Anki Çalışma Saati")]
        public int CalismaSaati { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Yapılan İşlemler")]
        public string YapilanIslemler { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Değişen Parçalar")]
        public string? DegisenParcalar { get; set; }

        [StringLength(100)]
        [Display(Name = "Bakımı Yapan / Firma")]
        public string? BakimiYapan { get; set; }

        [Display(Name = "Toplam Maliyet (TL)")]
        public decimal Maliyet { get; set; }

        [Display(Name = "Sonraki Bakım Tarihi")]
        public DateTime? SonrakiBakimTarihi { get; set; }

        [Display(Name = "Sonraki Bakım Çalışma Saati")]
        public int? SonrakiBakimCalismaSaati { get; set; }

        [StringLength(255)]
        [Display(Name = "Makbuz / Fatura Dosyası")]
        public string? MakbuzDosyaYolu { get; set; }

        [Display(Name = "Makbuzdan Okunan Metin (OCR)")]
        public string? MakbuzMetni { get; set; }

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }
}
