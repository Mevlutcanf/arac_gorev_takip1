using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class Makine
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Makine Adı")]
        public string Ad { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Marka")]
        public string? Marka { get; set; }

        [StringLength(50)]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [StringLength(50)]
        [Display(Name = "Seri No / Kod")]
        public string? SeriNo { get; set; }

        [StringLength(100)]
        [Display(Name = "Lokasyon / Departman")]
        public string? Lokasyon { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool Aktif { get; set; } = true;

        [Display(Name = "Güncel Çalışma Saati (Saat)")]
        public int CalismaSaati { get; set; } = 0;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }
}
