using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AracGorevFormu.Services
{
    public interface IEmailService
    {
        Task<(bool Basarili, string Mesaj)> TestEtAsync(SmtpAyarlarViewModel ayarlar);
        Task FormBildirimiGonderAsync(GorevFormu form);
        Task FormDurumDegisiklikBildirimiGonderAsync(GorevFormu form, bool onaylandi);
        Task FormTamamlandiBildirimiGonderAsync(GorevFormu form);
        SmtpAyari AyarlariGetir();
        void AyarlariKaydet(SmtpAyarlarViewModel model);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly AppDbContext _db;

        public EmailService(ILogger<EmailService> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public SmtpAyari AyarlariGetir()
        {
            var ayar = _db.SmtpAyarlari.FirstOrDefault(a => a.Id == 1);
            if (ayar == null)
            {
                ayar = new SmtpAyari { Id = 1, SmtpServer = "smtp.gmail.com", Port = 587, EnableSsl = true, Aktif = false };
                _db.SmtpAyarlari.Add(ayar);
                _db.SaveChanges();
            }
            return ayar;
        }

        public void AyarlariKaydet(SmtpAyarlarViewModel model)
        {
            var ayar = AyarlariGetir();
            ayar.SmtpServer = string.IsNullOrWhiteSpace(model.SmtpServer) ? "smtp.gmail.com" : model.SmtpServer;
            ayar.Port = model.Port <= 0 ? 587 : model.Port;
            ayar.EnableSsl = model.EnableSsl;
            ayar.SenderEmail = model.SenderEmail ?? string.Empty;

            // Şifre sadece gerçekten yeni bir şifre girildiyse güncelle (maskelenmiş placeholder gönderilmişse dokunma)
            if (!string.IsNullOrWhiteSpace(model.SenderPassword) && model.SenderPassword != "••••••••")
            {
                ayar.SenderPassword = model.SenderPassword;
            }

            ayar.NotificationEmails = model.NotificationEmails ?? string.Empty;
            ayar.Aktif = model.Aktif;

            _db.SaveChanges();
        }

        public async Task<(bool Basarili, string Mesaj)> TestEtAsync(SmtpAyarlarViewModel ayarlar)
        {
            if (string.IsNullOrWhiteSpace(ayarlar.SenderEmail))
            {
                return (false, "❌ Gönderen e-posta adresi boş olamaz.");
            }

            // Eğer şifre maskelenmişse veritabanındaki gerçek şifreyi kullan
            var gercekSifre = ayarlar.SenderPassword;
            if (string.IsNullOrWhiteSpace(gercekSifre) || gercekSifre == "••••••••")
            {
                var dbAyar = AyarlariGetir();
                gercekSifre = dbAyar.SenderPassword;
            }

            if (string.IsNullOrWhiteSpace(gercekSifre))
            {
                return (false, "❌ E-posta şifresi/uygulama şifresi tanımlanmamış.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Abdurrahman Tatlıcı | Fleon", ayarlar.SenderEmail));

                var alicilar = (ayarlar.NotificationEmails ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                if (alicilar.Count > 0)
                {
                    foreach (var alici in alicilar)
                    {
                        message.To.Add(new MailboxAddress(null, alici));
                    }
                }
                else
                {
                    message.To.Add(new MailboxAddress(null, ayarlar.SenderEmail));
                }

                message.Subject = "SMTP Test Mesajı — Fleon";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = @"
                        <div style='font-family: ""Segoe UI"", Arial, sans-serif; padding: 30px; max-width: 500px; margin: 0 auto; background-color: #f8fafc;'>
                            <div style='background: #0f172a; padding: 20px 25px; border-radius: 12px 12px 0 0; border-bottom: 4px solid #f59e0b;'>
                                <h2 style='color: #f8fafc; margin: 0; font-size: 20px;'><span style='color: #f59e0b;'>✅</span> SMTP Bağlantı Testi Başarılı</h2>
                            </div>
                            <div style='background: #ffffff; padding: 25px; border: 1px solid #e2e8f0; border-radius: 0 0 12px 12px;'>
                                <p style='color: #334155; margin: 0 0 10px;'>Fleon sisteminden gelen e-posta bildirim testi başarıyla tamamlandı.</p>
                                <p style='color: #94a3b8; font-size: 12px; margin: 0;'>Bu mesaj otomatik test amaçlı gönderilmiştir.</p>
                            </div>
                        </div>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 15000; // 15 saniye timeout

                // Port'a göre uygun güvenlik protokolünü seç
                var secureSocketOptions = ayarlar.EnableSsl
                    ? (ayarlar.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await client.ConnectAsync(ayarlar.SmtpServer, ayarlar.Port, secureSocketOptions);
                await client.AuthenticateAsync(ayarlar.SenderEmail, gercekSifre);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("SMTP test e-postası başarıyla gönderildi -> {Email}", ayarlar.SenderEmail);
                return (true, "✅ Test e-postası başarıyla gönderildi! Gelen kutunuzu kontrol ediniz.");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "SMTP kimlik doğrulama hatası.");
                return (false, "❌ Kimlik doğrulama hatası: Kullanıcı adı veya şifre hatalı. Gmail kullanıyorsanız 'Uygulama Şifresi' oluşturmanız gerekiyor.");
            }
            catch (MailKit.Security.SslHandshakeException ex)
            {
                _logger.LogError(ex, "SMTP SSL/TLS bağlantı hatası.");
                return (false, "❌ SSL/TLS bağlantı hatası: Sunucu güvenli bağlantıyı reddetti. Port ve SSL ayarlarını kontrol ediniz.");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError(ex, "SMTP sunucusuna bağlanılamadı.");
                return (false, $"❌ Sunucuya bağlanılamadı: '{ayarlar.SmtpServer}:{ayarlar.Port}' adresine erişilemiyor. Sunucu adresi ve port'u kontrol ediniz.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "SMTP bağlantı zaman aşımı.");
                return (false, "❌ Bağlantı zaman aşımına uğradı. Sunucu yanıt vermiyor, lütfen ağ bağlantınızı ve sunucu bilgilerinizi kontrol ediniz.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP test gönderimi başarısız: {Hata}", ex.Message);
                return (false, $"❌ E-posta gönderilemedi: {ex.Message}");
            }
        }

        public async Task FormBildirimiGonderAsync(GorevFormu form)
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrWhiteSpace(ayar.SenderEmail) || string.IsNullOrWhiteSpace(ayar.SenderPassword))
            {
                return;
            }

            try
            {
                var alicilar = (ayar.NotificationEmails ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                if (alicilar.Count == 0)
                {
                    alicilar.Add(ayar.SenderEmail);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Abdurrahman Tatlıcı | Fleon", ayar.SenderEmail));

                foreach (var alici in alicilar)
                {
                    message.To.Add(new MailboxAddress(null, alici));
                }

                message.Subject = $"[Yeni Görev Talebi] {form.AracPlaka} - {form.KullananAdSoyad} ({form.TakipKodu})";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: ""Segoe UI"", Arial, sans-serif; padding: 20px; color: #1e293b; max-width: 600px; margin: 0 auto; background-color: #f8fafc;'>
                            <div style='background: #0f172a; padding: 20px 25px; border-radius: 12px 12px 0 0; border-bottom: 4px solid #f59e0b;'>
                                <h2 style='color: #f8fafc; margin: 0; font-size: 20px;'><span style='color: #f59e0b;'>🚗</span> Yeni Araç Görev Talebi</h2>
                            </div>
                            <div style='background: #ffffff; padding: 25px; border: 1px solid #e2e8f0;'>
                                <p style='color: #475569;'>Sistemde yeni bir araç görev formu dolduruldu. Detaylar aşağıdadır:</p>
                                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold; width: 40%;'>Takip Kodu:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'><b style='color: #d97706; font-family: monospace; font-size: 16px;'>{form.TakipKodu}</b></td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Araç:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.AracPlaka} — {form.AracMarka} {form.AracModel}</td></tr>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Kullanan Personel:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.KullananAdSoyad} ({form.KullananTelefon})</td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Görev Amacı:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.GorevAmaci}</td></tr>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Planlanan Çıkış:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.CikisZamani:dd.MM.yyyy HH:mm}</td></tr>
                                </table>
                            </div>
                            <div style='background: #f1f5f9; padding: 15px 25px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0; border-top: none;'>
                                <p style='color: #64748b; font-size: 13px; margin: 0;'>Yönetim panelinizden bu talebi inceleyebilir ve onaylayabilirsiniz.</p>
                            </div>
                        </div>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 15000;

                var secureSocketOptions = ayar.EnableSsl
                    ? (ayar.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await client.ConnectAsync(ayar.SmtpServer, ayar.Port, secureSocketOptions);
                await client.AuthenticateAsync(ayar.SenderEmail, ayar.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Yeni form e-posta bildirimi gönderildi -> {TakipKodu}", form.TakipKodu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Form bildirim e-postası gönderilirken hata oluştu: {Hata}", ex.Message);
            }
        }

        public async Task FormDurumDegisiklikBildirimiGonderAsync(GorevFormu form, bool onaylandi)
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrWhiteSpace(ayar.SenderEmail) || string.IsNullOrWhiteSpace(ayar.SenderPassword))
            {
                return;
            }

            try
            {
                var alicilar = (ayar.NotificationEmails ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                if (alicilar.Count == 0)
                {
                    alicilar.Add(ayar.SenderEmail);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Abdurrahman Tatlıcı | Fleon", ayar.SenderEmail));

                foreach (var alici in alicilar)
                {
                    message.To.Add(new MailboxAddress(null, alici));
                }

                var durumBaslik = onaylandi ? "✅ Araç Görevi Onaylandı & Çıkış Yaptı" : "❌ Görev Talebi Reddedildi";
                var durumRenk = onaylandi ? "#166534" : "#991b1b";
                var durumBg = onaylandi ? "#dcfce7" : "#fee2e2";

                message.Subject = $"[{durumBaslik}] {form.AracPlaka} - {form.KullananAdSoyad} ({form.TakipKodu})";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: ""Segoe UI"", Arial, sans-serif; padding: 20px; color: #1e293b; max-width: 600px; margin: 0 auto; background-color: #f8fafc;'>
                            <div style='background: #0f172a; padding: 20px 25px; border-radius: 12px 12px 0 0; border-bottom: 4px solid {durumRenk};'>
                                <h2 style='color: #f8fafc; margin: 0; font-size: 20px;'>{durumBaslik}</h2>
                            </div>
                            <div style='background: #ffffff; padding: 25px; border: 1px solid #e2e8f0;'>
                                <div style='background: {durumBg}; color: {durumRenk}; padding: 12px 15px; border-radius: 6px; font-weight: bold; margin-bottom: 20px;'>
                                    {(onaylandi ? "Görev formu yönetici tarafından onaylanmış olup, aracın şirketten çıkışı yapılmıştır." : $"Görev formu reddedilmiştir. Neden: {form.RedNedeni}")}
                                </div>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold; width: 40%;'>Takip Kodu:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'><b style='color: #d97706; font-family: monospace; font-size: 16px;'>{form.TakipKodu}</b></td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Araç:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.AracPlaka} — {form.AracMarka} {form.AracModel}</td></tr>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Kullanan Personel:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.KullananAdSoyad} ({form.KullananTelefon})</td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Onaylayan/İşlem Yapan:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.OnaylayanKullaniciAdi ?? "Yönetici"} ({form.OnayTarihi:dd.MM.yyyy HH:mm})</td></tr>
                                </table>
                            </div>
                        </div>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 15000;

                var secureSocketOptions = ayar.EnableSsl
                    ? (ayar.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await client.ConnectAsync(ayar.SmtpServer, ayar.Port, secureSocketOptions);
                await client.AuthenticateAsync(ayar.SenderEmail, ayar.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Form durum değişikliği e-posta bildirimi gönderildi -> {TakipKodu}", form.TakipKodu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Form durum bildirim e-postası gönderilirken hata oluştu: {Hata}", ex.Message);
            }
        }

        public async Task FormTamamlandiBildirimiGonderAsync(GorevFormu form)
        {
            var ayar = AyarlariGetir();
            if (!ayar.Aktif || string.IsNullOrWhiteSpace(ayar.SenderEmail) || string.IsNullOrWhiteSpace(ayar.SenderPassword))
            {
                return;
            }

            try
            {
                var alicilar = (ayar.NotificationEmails ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                if (alicilar.Count == 0)
                {
                    alicilar.Add(ayar.SenderEmail);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Abdurrahman Tatlıcı | Fleon", ayar.SenderEmail));

                foreach (var alici in alicilar)
                {
                    message.To.Add(new MailboxAddress(null, alici));
                }

                message.Subject = $"[🏁 Görev Tamamlandı] {form.AracPlaka} - {form.KullananAdSoyad}";

                TimeSpan sure = (form.GercekDonusZamani ?? DateTime.Now) - form.CikisZamani;
                string gecenSure = "";
                if (sure.Days > 0) gecenSure += $"{sure.Days} Gün ";
                if (sure.Hours > 0) gecenSure += $"{sure.Hours} Saat ";
                if (sure.Minutes > 0 || sure.TotalMinutes < 1) gecenSure += $"{sure.Minutes} Dakika";
                gecenSure = gecenSure.Trim();

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: ""Segoe UI"", Arial, sans-serif; padding: 20px; color: #1e293b; max-width: 600px; margin: 0 auto; background-color: #f8fafc;'>
                            <div style='background: #0f172a; padding: 20px 25px; border-radius: 12px 12px 0 0; border-bottom: 4px solid #f59e0b;'>
                                <h2 style='color: #f8fafc; margin: 0; font-size: 20px;'><span style='color: #f59e0b;'>🏁</span> Araç Şirkete Teslim Edildi</h2>
                            </div>
                            <div style='background: #ffffff; padding: 25px; border: 1px solid #e2e8f0;'>
                                <div style='background: #f0fdf4; color: #166534; padding: 12px 15px; border-radius: 6px; font-weight: bold; margin-bottom: 20px;'>
                                    Araç görevi başarıyla tamamlanmış ve araç geri teslim edilmiştir.
                                </div>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold; width: 40%;'>Takip Kodu:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'><b style='color: #d97706; font-family: monospace; font-size: 16px;'>{form.TakipKodu}</b></td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Araç:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.AracPlaka} — {form.AracMarka} {form.AracModel}</td></tr>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Kullanan Personel:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.KullananAdSoyad} ({form.KullananTelefon})</td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Çıkış Saati:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{form.CikisZamani:dd.MM.yyyy HH:mm}</td></tr>
                                    <tr style='background: #f8fafc;'><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Dönüş Saati:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'>{((DateTime)(form.GercekDonusZamani ?? DateTime.Now)).ToString("dd.MM.yyyy HH:mm")}</td></tr>
                                    <tr><td style='padding: 10px 12px; border: 1px solid #cbd5e1; font-weight: bold;'>Görev Süresi:</td><td style='padding: 10px 12px; border: 1px solid #cbd5e1;'><b>{gecenSure}</b></td></tr>
                                </table>
                            </div>
                        </div>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 15000;

                var secureSocketOptions = ayar.EnableSsl
                    ? (ayar.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await client.ConnectAsync(ayar.SmtpServer, ayar.Port, secureSocketOptions);
                await client.AuthenticateAsync(ayar.SenderEmail, ayar.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Araç teslim edildi e-posta bildirimi gönderildi -> {TakipKodu}", form.TakipKodu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç teslim edildi bildirim e-postası gönderilirken hata oluştu: {Hata}", ex.Message);
            }
        }
    }
}
