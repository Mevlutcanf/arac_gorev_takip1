using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.EntityFrameworkCore;

namespace AracGorevFormu.Services
{
    public class AracVadeBildirimService : BackgroundService
    {
        private readonly ILogger<AracVadeBildirimService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AracVadeBildirimService(ILogger<AracVadeBildirimService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Servis ilk başladığında 2 dakika bekle (uygulama tam ayağa kalksın)
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Araç vade (muayene/sigorta) kontrolü başlatılıyor...");
                    await AracVadeleriniKontrolEt(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Araç vade kontrolü sırasında bir hata oluştu.");
                }

                // 24 saatte bir çalış
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task AracVadeleriniKontrolEt(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var bugun = DateTime.Today;

            var aktifAraclar = await dbContext.Vehicles.Where(v => v.Aktif).ToListAsync(cancellationToken);

            foreach (var arac in aktifAraclar)
            {
                // 1. MUAYENE KONTROLÜ
                if (arac.MuayeneBitisTarihi.HasValue)
                {
                    var muayeneBitis = arac.MuayeneBitisTarihi.Value.Date;
                    var kalanGun = (muayeneBitis - bugun).Days;

                    // Eğer süre 30 gün veya daha azsa VE bildirim henüz GÖNDERİLMEDİYSE
                    if (kalanGun <= 30 && kalanGun >= -365) 
                    {
                        if (!arac.MuayeneBildirimGonderildi)
                        {
                            await emailService.AracVadeBildirimiGonderAsync(arac, "Muayene", kalanGun);
                            arac.MuayeneBildirimGonderildi = true;
                            dbContext.Vehicles.Update(arac);
                        }
                    }
                    // Eğer kullanıcı tarihi güncellediyse (muayeneyi yaptırdıysa) ve süre 30 günden fazlaysa
                    else if (kalanGun > 30 && arac.MuayeneBildirimGonderildi)
                    {
                        // Bayrağı sıfırla ki seneye tekrar mail atılabilsin
                        arac.MuayeneBildirimGonderildi = false;
                        dbContext.Vehicles.Update(arac);
                    }
                }

                // 2. SİGORTA KONTROLÜ
                if (arac.SigortaBitisTarihi.HasValue)
                {
                    var sigortaBitis = arac.SigortaBitisTarihi.Value.Date;
                    var kalanGun = (sigortaBitis - bugun).Days;

                    if (kalanGun <= 30 && kalanGun >= -365)
                    {
                        if (!arac.SigortaBildirimGonderildi)
                        {
                            await emailService.AracVadeBildirimiGonderAsync(arac, "Sigorta", kalanGun);
                            arac.SigortaBildirimGonderildi = true;
                            dbContext.Vehicles.Update(arac);
                        }
                    }
                    else if (kalanGun > 30 && arac.SigortaBildirimGonderildi)
                    {
                        arac.SigortaBildirimGonderildi = false;
                        dbContext.Vehicles.Update(arac);
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Araç vade kontrolü tamamlandı.");
        }
    }
}
