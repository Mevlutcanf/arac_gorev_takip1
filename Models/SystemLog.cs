using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string IslemTuru { get; set; } = string.Empty;

        [Required]
        public string Detay { get; set; } = string.Empty;
    }
}
