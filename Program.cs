using AracGorevFormu.Data;
using AracGorevFormu.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// IP adresi dogru alinmasi icin proxy/forwarded headers destegi
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// EF Core Veritabanı Kurulumu (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırılmamış.");
    options.UseSqlServer(connectionString);
});

// Repository Katmani (EF Core Scoped Injection)
builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddScoped<GorevFormuRepository>();
builder.Services.AddScoped<AdminUserRepository>();

// Servisler
builder.Services.AddHttpClient();
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

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
