using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class GorevFormu
    {
        public int Id { get; set; }

        /// <summary>
        /// Giriş yapmadan formu dolduran kişinin, daha sonra durumunu sorgulayabilmesi
        /// ve dönüş bildirimi yapabilmesi için kullandığı benzersiz takip kodu.
        /// </summary>
        public string TakipKodu { get; set; } = string.Empty;

        [Required]
        public int VehicleId { get; set; }

        // Formun oluşturulduğu andaki araç bilgisi (araç ileride silinse bile geçmiş kaybolmasın diye anlık kopya)
        public string AracPlaka { get; set; } = string.Empty;
        public string AracMarka { get; set; } = string.Empty;
        public string AracModel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Kullanan Ad Soyad")]
        [StringLength(100)]
        [MinLength(5, ErrorMessage = "Ad Soyad en az 5 karakter olmalıdır.")]
        public string KullananAdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [Display(Name = "Telefon")]
        [RegularExpression(@"^0?5\d{2}\s?\d{3}\s?\d{2}\s?\d{2}$", ErrorMessage = "Geçerli bir cep telefonu numarası giriniz. Örnek: 05XX XXX XX XX")]
        [StringLength(20)]
        public string KullananTelefon { get; set; } = string.Empty;

        [Display(Name = "Departman / Birim")]
        [StringLength(80)]
        public string? Departman { get; set; }

        [Required(ErrorMessage = "Görev amacı zorunludur.")]
        [Display(Name = "Görev Amacı / Gidilecek Yer")]
        [StringLength(500)]
        [MinLength(10, ErrorMessage = "Görev amacı en az 10 karakter olmalıdır.")]
        public string GorevAmaci { get; set; } = string.Empty;

        [Required(ErrorMessage = "Çıkış tarihi/saati zorunludur.")]
        [Display(Name = "Planlanan Çıkış Tarihi/Saati")]
        [DataType(DataType.DateTime)]
        public DateTime CikisZamani { get; set; }

        [Required(ErrorMessage = "Planlanan dönüş tarihi/saati zorunludur.")]
        [Display(Name = "Planlanan Dönüş Tarihi/Saati")]
        [DataType(DataType.DateTime)]
        public DateTime PlanlananDonusZamani { get; set; }

        /// <summary>Aracın fiilen geri teslim edildiği an. Null ise araç hâlâ dışarıda.</summary>
        [Display(Name = "Gerçekleşen Dönüş Tarihi/Saati")]
        public DateTime? GercekDonusZamani { get; set; }

        [Display(Name = "Çıkış Kilometresi")]
        public int? CikisKm { get; set; }

        [Display(Name = "Dönüş Kilometresi")]
        public int? DonusKm { get; set; }

        public GorevDurumu Durum { get; set; } = GorevDurumu.Beklemede;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        public string? OnaylayanKullaniciAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
        public string? RedNedeni { get; set; }

        public string DurumMetni => Durum switch
        {
            GorevDurumu.Beklemede => "Onay Bekliyor",
            GorevDurumu.Onaylandi => (GercekDonusZamani == null ? "Onaylandı - Araç Dışarıda" : "Onaylandı"),
            GorevDurumu.Reddedildi => "Reddedildi",
            GorevDurumu.TamamlandiDondu => "Tamamlandı - Araç Teslim Edildi",
            _ => Durum.ToString()
        };

        public string DurumRengi => Durum switch
        {
            GorevDurumu.Beklemede => "warning",
            GorevDurumu.Onaylandi => (GercekDonusZamani == null ? "primary" : "success"),
            GorevDurumu.Reddedildi => "danger",
            GorevDurumu.TamamlandiDondu => "success",
            _ => "secondary"
        };

        public bool AracDisarida => Durum == GorevDurumu.Onaylandi && GercekDonusZamani == null;
    }
}
