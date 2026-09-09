using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Ad { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Tur { get; set; } = string.Empty; // "MakineLokasyon", "BakimTuru", "SahiplikTuru" vb.
    }
}
