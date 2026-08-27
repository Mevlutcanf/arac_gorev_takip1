using AracGorevFormu.Data;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core Veritabanı Kurulumu (Varsayılan: Kurulum gerektirmeyen tak-çalıştır İlişkisel Veritabanı)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/AracGorevFormu.db";
    if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Repository Katmani (EF Core Scoped Injection)
builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddScoped<GorevFormuRepository>();
builder.Services.AddScoped<AdminUserRepository>();

// Servisler
builder.Services.AddScoped<ISmsService, DummySmsService>();
builder.Services.AddScoped<ArventoService>();
builder.Services.AddScoped<IArventoService>(sp => sp.GetRequiredService<ArventoService>());
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHgsService, HgsService>();

// Kimlik dogrulama: Sadece admin panelini korumak icin Cookie tabanli auth.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/ErisimYok";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AracGorevFormu.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Baslangic verisi ve otomatik veritabanı tablo kurulumu
SeedData.Uygula(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
