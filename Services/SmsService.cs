namespace AracGorevFormu.Services
{
    /// <summary>
    /// SMS gönderimi için soyut arayüz.
    /// Ücretli bir SMS sağlayıcı (NetGSM, İleti Merkezi, Twilio vb.) yapılandırıldığında
    /// bu arayüzü implemente eden somut sınıf kullanılır.
    /// </summary>
    public interface ISmsService
    {
        Task<bool> SmsGonderAsync(string telefonNumarasi, string mesaj);
        bool Yapilandirildi { get; }
    }

    /// <summary>
    /// Varsayılan SMS servisi - SMS gönderimi yapılandırılmadığında kullanılır.
    /// Loglama yapar ama gerçek SMS göndermez.
    /// 
    /// Gerçek bir SMS sağlayıcıya geçmek için:
    /// 1. NetGSM, İleti Merkezi veya Twilio hesabı açın
    /// 2. Bu sınıfı kaldırıp yerine gerçek implementasyonu yazın
    /// 3. Program.cs'de DI kaydını güncelleyin
    /// </summary>
    public class DummySmsService : ISmsService
    {
        private readonly ILogger<DummySmsService> _logger;

        public DummySmsService(ILogger<DummySmsService> logger)
        {
            _logger = logger;
        }

        public bool Yapilandirildi => false;

        public Task<bool> SmsGonderAsync(string telefonNumarasi, string mesaj)
        {
            _logger.LogInformation(
                "SMS gönderimi yapılandırılmadı. Gönderilecek mesaj -> {Telefon}: {Mesaj}",
                telefonNumarasi, mesaj);
            return Task.FromResult(false);
        }
    }
}
