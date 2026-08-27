namespace AracGorevFormu.Models.ViewModels
{
    public class SistemAyarlariPageViewModel
    {
        public string AktifTab { get; set; } = "profil";
        public ProfilViewModel ProfilModel { get; set; } = new();
        public List<AdminUser> Yoneticiler { get; set; } = new();
        public YeniAdminViewModel YeniYoneticiModel { get; set; } = new();
        public SmtpAyarlarViewModel SmtpModel { get; set; } = new();
        public ArventoAyarlarViewModel ArventoModel { get; set; } = new();
    }
}
