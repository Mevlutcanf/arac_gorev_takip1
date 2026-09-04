namespace AracGorevFormu.Models
{
    public class SmtpAyari
    {
        public int Id { get; set; }
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string NotificationEmails { get; set; } = string.Empty;
        public bool Aktif { get; set; } = false;
    }

    public class ArventoAyari
    {
        public int Id { get; set; }
        public string ApiUrl { get; set; } = "https://ws.arvento.com/v1/report.asmx";
        public string? KullaniciAdi { get; set; }
        public string? Sifre { get; set; }
        public string? ApiKey { get; set; }
        public bool Aktif { get; set; } = false;
    }
}
