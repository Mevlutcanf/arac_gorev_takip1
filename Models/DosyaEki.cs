using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    /// <summary>
    /// Genel amaçlı dosya eki entity.
    /// Ruhsat, makbuz, fatura gibi dosyaları veritabanında (byte[]) saklamak için kullanılır.
    /// </summary>
    public class DosyaEki
    {
        public int Id { get; set; }

        /// <summary>Hangi varlık türüne ait: "Ruhsat", "MakineBakimMakbuzu" vb.</summary>
        [Required]
        [StringLength(50)]
        public string ParentTuru { get; set; } = string.Empty;

        /// <summary>İlgili varlığın Id'si (Vehicle.Id, MakineBakim.Id vb.)</summary>
        public int ParentId { get; set; }

        /// <summary>Orijinal dosya adı</summary>
        [Required]
        [StringLength(255)]
        public string DosyaAdi { get; set; } = string.Empty;

        /// <summary>MIME tipi (application/pdf, image/jpeg vb.)</summary>
        [StringLength(100)]
        public string DosyaTipi { get; set; } = "application/octet-stream";

        /// <summary>Dosya içeriği (binary)</summary>
        [Required]
        public byte[] Icerik { get; set; } = Array.Empty<byte>();

        public DateTime YuklenmeTarihi { get; set; } = DateTime.Now;
    }
}
