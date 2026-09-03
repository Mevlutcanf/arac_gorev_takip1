using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models.ViewModels
{
    public class YeniGorevFormuViewModel
    {
        [Required(ErrorMessage = "Lütfen bir araç seçiniz.")]
        [Display(Name = "Araç (Plaka - Marka Model)")]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir araç seçiniz.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Kullanan Ad Soyad")]
        [StringLength(100)]
        [MinLength(5, ErrorMessage = "Ad Soyad en az 5 karakter olmalıdır (ad ve soyad).")]
        public string KullananAdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [Display(Name = "Telefon")]
        [StringLength(20)]
        [RegularExpression(@"^0?5\d{2}\s?\d{3}\s?\d{2}\s?\d{2}$", ErrorMessage = "Geçerli bir cep telefonu numarası giriniz. Örnek: 05XX XXX XX XX")]
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
        public DateTime CikisZamani { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);


        public List<Vehicle> AktifAraclar { get; set; } = new();
    }

    public class SorgulaViewModel
    {
        public string SorgulamaTipi { get; set; } = "kod";

        [Display(Name = "Takip Kodu")]
        public string? TakipKodu { get; set; }

        [Display(Name = "Ad Soyad")]
        public string? AdSoyad { get; set; }

        [Display(Name = "Telefon Numarası")]
        public string? Telefon { get; set; }

        public List<GorevFormu>? SonucListesi { get; set; }

        public bool SonucGeldi { get; set; } = false;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        public string? HataMesaji { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int ToplamArac { get; set; }
        public int AktifArac { get; set; }
        public int SuAndaDisaridaOlan { get; set; }
        public int SuAndaIcerideOlan { get; set; }
        public int BekleyenOnaySayisi { get; set; }
        public int BugunCikanSayisi { get; set; }
        public List<GorevFormu> SonFormlar { get; set; } = new();
        public List<GorevFormu> DisaridakiAraclar { get; set; } = new();
        public List<Vehicle> IceridekiAraclar { get; set; } = new();
    }

    public class YeniAdminViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yetki Rolü seçimi zorunludur.")]
        [Display(Name = "Yetki Rolü")]
        public string Rol { get; set; } = "Yönetici";

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;
    }

    public class RedViewModel
    {
        public int FormId { get; set; }

        [Required(ErrorMessage = "Lütfen ret nedenini belirtiniz.")]
        [Display(Name = "Ret Nedeni")]
        [StringLength(300)]
        public string RedNedeni { get; set; } = string.Empty;
    }

    public class FiloHgsDashboardViewModel
    {
        public decimal ToplamFiloBorcu { get; set; }
        public int ToplamOdenmeyenGecisSayisi { get; set; }
        public int ToplamCezaSayisi { get; set; }
        public int BorcluAracSayisi { get; set; }
        public int ToplamAracSayisi { get; set; }
        public List<AracHgsOzetViewModel> AracOzetleri { get; set; } = new();
        public List<HgsGecis> TumGecisler { get; set; } = new();
        public string? SeciliPlaka { get; set; }
    }

    public class AracHgsOzetViewModel
    {
        public int VehicleId { get; set; }
        public string Plaka { get; set; } = string.Empty;
        public string MarkaModel { get; set; } = string.Empty;
        public string? Sürücü { get; set; }
        public string? Lokasyon { get; set; }
        public decimal ToplamBorc { get; set; }
        public int OdenmeyenAdet { get; set; }
        public int CezaAdet { get; set; }
        public List<HgsGecis> Gecisler { get; set; } = new();
    }
}
