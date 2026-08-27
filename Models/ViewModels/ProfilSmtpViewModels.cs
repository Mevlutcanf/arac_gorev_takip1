using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models.ViewModels
{
    public class ProfilViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Display(Name = "Mevcut Şifre")]
        [DataType(DataType.Password)]
        public string? MevcutSifre { get; set; }

        [Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string? YeniSifre { get; set; }

        [Display(Name = "Yeni Şifre Tekrar")]
        [DataType(DataType.Password)]
        [Compare("YeniSifre", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string? YeniSifreTekrar { get; set; }
    }

    public class SmtpAyarlarViewModel
    {
        [Required(ErrorMessage = "SMTP Sunucu adresi zorunludur.")]
        [Display(Name = "SMTP Sunucu (Host)")]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [Required(ErrorMessage = "Port zorunludur.")]
        [Display(Name = "SMTP Port")]
        public int Port { get; set; } = 587;

        [Display(Name = "SSL/TLS Kullan")]
        public bool EnableSsl { get; set; } = true;

        [Required(ErrorMessage = "Gönderen e-posta zorunludur.")]
        [Display(Name = "Gönderen E-Posta (Kullanıcı Adı)")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string SenderEmail { get; set; } = string.Empty;

        [Display(Name = "E-Posta Şifresi / Uygulama Şifresi")]
        [DataType(DataType.Password)]
        public string SenderPassword { get; set; } = string.Empty;

        [Display(Name = "Bildirim Alacak E-Posta Adresleri (Virgülle ayırın)")]
        public string NotificationEmails { get; set; } = string.Empty;

        [Display(Name = "Yeni Form Gönderildiğinde E-Posta Bildirimi Gönder")]
        public bool Aktif { get; set; } = false;

        public string? TestSonucu { get; set; }
    }

    public class ArventoAyarlarViewModel
    {
        [Display(Name = "Arvento API URL")]
        public string ApiUrl { get; set; } = "https://ws.arvento.com/v1/report.asmx";

        [Display(Name = "Kullanıcı Adı")]
        public string? KullaniciAdi { get; set; }

        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        public string? Sifre { get; set; }

        [Display(Name = "API Anahtarı")]
        public string? ApiKey { get; set; }

        public bool Aktif { get; set; } = false;

        public string? TestSonucu { get; set; }
    }
}
