using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Yetki Rolü")]
        public string Rol { get; set; } = "Yönetici";

        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        /// <summary>
        /// Süper admin, diğer adminleri silemez/kendini silemez gibi basit korumalar için.
        /// İlk oluşturulan admin hesabı bu şekilde işaretlenir.
        /// </summary>
        public bool AnaYonetici { get; set; } = false;
    }
}
