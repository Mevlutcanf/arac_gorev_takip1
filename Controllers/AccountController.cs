using System.Security.Claims;
using AracGorevFormu.Data;
using AracGorevFormu.Models;
using AracGorevFormu.Models.ViewModels;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AracGorevFormu.Controllers
{
    public class AccountController : Controller
    {
        private readonly AdminUserRepository _adminRepo;
        private readonly AppDbContext _db;

        public AccountController(AdminUserRepository adminRepo, AppDbContext db)
        {
            _adminRepo = adminRepo;
            _db = db;
        }

        private string GetClientIpAddress()
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip)) ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ip == "::1" || ip == "127.0.0.1") return "Localhost";
            return string.IsNullOrEmpty(ip) ? "Bilinmiyor" : ip;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _adminRepo.GetirByKullaniciAdiAsync(model.KullaniciAdi);
            if (admin == null || !PasswordHasher.Dogrula(model.Sifre, admin.PasswordHash, admin.PasswordSalt))
            {
                model.HataMesaji = "Kullanıcı adı veya şifre hatalı.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.KullaniciAdi),
                new Claim("AdSoyad", admin.AdSoyad),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            _db.SystemLogs.Add(new SystemLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = admin.KullaniciAdi,
                IslemTuru = "Giriş Yapıldı",
                Detay = "Yönetici sisteme başarılı şekilde giriş yaptı.",
                IpAdresi = GetClientIpAddress()
            });
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var kullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            _db.SystemLogs.Add(new SystemLog
            {
                Tarih = DateTime.Now,
                KullaniciAdi = kullaniciAdi,
                IslemTuru = "Çıkış Yapıldı",
                Detay = "Yönetici sistemden güvenli çıkış yaptı.",
                IpAdresi = GetClientIpAddress()
            });
            await _db.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ErisimYok()
        {
            return View();
        }
    }
}
