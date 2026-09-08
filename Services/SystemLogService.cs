using AracGorevFormu.Data;
using AracGorevFormu.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AracGorevFormu.Services
{
    public interface ISystemLogService
    {
        Task LogIslemAsync(string kullaniciAdi, string islemTuru, string detay, string? ipAdresi = null);
        Task LogIslemWithHttpContextAsync(string islemTuru, string detay, HttpContext httpContext, string? fallbackKullaniciAdi = null);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly AppDbContext _db;

        public SystemLogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogIslemAsync(string kullaniciAdi, string islemTuru, string detay, string? ipAdresi = null)
        {
            var log = new SystemLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = string.IsNullOrWhiteSpace(kullaniciAdi) ? "Sistem" : kullaniciAdi,
                IslemTuru = islemTuru,
                Detay = detay,
                IpAdresi = ipAdresi ?? "Bilinmiyor"
            };

            _db.SystemLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task LogIslemWithHttpContextAsync(string islemTuru, string detay, HttpContext httpContext, string? fallbackKullaniciAdi = null)
        {
            string kullaniciAdi = httpContext.User.Identity?.Name ?? fallbackKullaniciAdi ?? "Bilinmiyor";
            string ipAdresi = GetClientIpAddress(httpContext);

            await LogIslemAsync(kullaniciAdi, islemTuru, detay, ipAdresi);
        }

        private string GetClientIpAddress(HttpContext httpContext)
        {
            var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip)) ip = httpContext.Connection.RemoteIpAddress?.ToString();
            if (ip == "::1" || ip == "127.0.0.1") return "Localhost";
            return string.IsNullOrEmpty(ip) ? "Bilinmiyor" : ip;
        }
    }
}
