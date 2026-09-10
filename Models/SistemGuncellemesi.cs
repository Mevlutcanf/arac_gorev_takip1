using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class SistemGuncellemesi
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Versiyon numarası zorunludur.")]
        [StringLength(20)]
        public string Versiyon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(100)]
        public string Baslik { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik zorunludur.")]
        public string Icerik { get; set; } = string.Empty;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }
}
