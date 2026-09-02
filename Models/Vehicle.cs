using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Plaka zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Required(ErrorMessage = "Marka zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Marka")]
        public string Marka { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Renk")]
        public string? Renk { get; set; }

        [StringLength(30)]
        [Display(Name = "Sahiplik Türü")]
        public string SahiplikTuru { get; set; } = "Şirket Aracı"; // "Şirket Aracı" veya "Kiralık Araç"

        [StringLength(100)]
        [Display(Name = "Zimmetli / Sabit Sürücü")]
        public string? SabitSurucu { get; set; } // Örn: "Ahmet Yılmaz (İstanbul Bölge)"

        [StringLength(100)]
        [Display(Name = "Araç Lokasyonu / Şehir")]
        public string? Lokasyon { get; set; } = "Ankara Genel Merkez";

        public bool Aktif { get; set; } = true;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        // ---------- RUHSAT BİLGİLERİ ----------

        [StringLength(50)]
        [Display(Name = "Şasi No (VIN)")]
        public string? SasiNo { get; set; }

        [StringLength(50)]
        [Display(Name = "Motor No")]
        public string? MotorNo { get; set; }

        [Display(Name = "Trafik Tescil Tarihi")]
        public DateTime? TescilTarihi { get; set; }

        [Display(Name = "Trafik Muayene Bitiş Tarihi")]
        public DateTime? MuayeneBitisTarihi { get; set; }

        [Display(Name = "Kasko/Zorunlu Trafik Sigortası Bitiş Tarihi")]
        public DateTime? SigortaBitisTarihi { get; set; }

        [StringLength(255)]
        [Display(Name = "Ruhsat Belge Dosya Yolu")]
        public string? RuhsatDosyaYolu { get; set; }

        // ---------- ARVENTO CANLI VERİLERİ ----------

        [Display(Name = "Güncel Kilometre (Arvento)")]
        public int? GuncelKm { get; set; }

        [Display(Name = "Son Konum Zamanı (Arvento)")]
        public DateTime? SonKonumZamani { get; set; }

        [StringLength(255)]
        [Display(Name = "Son Adres (Arvento)")]
        public string? SonAdres { get; set; }
    }
}
