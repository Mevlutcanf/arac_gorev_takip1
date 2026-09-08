using System;
using System.ComponentModel.DataAnnotations;

namespace AracGorevFormu.Models
{
    public class MailTaslak
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Konu { get; set; } = string.Empty;

        [Required]
        public string Icerik { get; set; } = string.Empty;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }
}
