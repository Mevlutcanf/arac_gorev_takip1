using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models.ViewModels
{
    public class MailGonderViewModel
    {
        [Required(ErrorMessage = "Alıcı E-Posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir E-Posta adresi giriniz.")]
        public string AliciEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konu zorunludur.")]
        [StringLength(200)]
        public string Konu { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik zorunludur.")]
        public string Icerik { get; set; } = string.Empty;
    }
}
