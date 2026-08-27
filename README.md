# 🚗 Araç Görev Takip Sistemi (AracGorevFormu)

Şirket araçlarının kim tarafından, hangi amaçla, hangi tarih/saat aralığında kullanıldığını
dijital olarak takip etmek için geliştirilmiş **ASP.NET Core 8 MVC** uygulaması.

## Temel Özellikler

- ✅ **Giriş yapmadan görev formu doldurma** — herhangi bir personel, hesap açmadan formu doldurup gönderebilir.
- ✅ **Plaka + Marka/Model seçimi** ile araç seçimi (aktif araçlar listelenir).
- ✅ **Takip kodu sistemi** — form gönderildiğinde benzersiz bir kod (örn. `GF-438573`) üretilir; bu kodla
  kullanıcı daha sonra formunun durumunu sorgulayabilir ve **aracı geri teslim ettiğini kendisi bildirebilir**.
- ✅ **Admin onay mekanizması** — form, yetkili (admin) giriş yapıp onaylamadan geçerli sayılmaz.
  Onaylanmadan araç "kullanımda" statüsüne geçmez.
- ✅ **Çıkış / Dönüş takibi** — hangi aracın ne zaman çıktığı, ne zaman geri döndüğü hem admin panelinde
  hem de kullanıcının takip sayfasında entegre şekilde görünür (Bekliyor → Onaylandı/Dışarıda → Tamamlandı).
- ✅ **Admin paneli**
  - Dashboard: toplam/aktif araç, bekleyen onay, şu an dışarıda olan araç, bugün açılan form sayısı
  - Görev formlarını listeleme/filtreleme (Bekleyen / Onaylı-Dışarıda / Tamamlanan / Reddedilen)
  - Formu onaylama / reddetme (ret nedeni ile)
  - Aracın dönüşünü admin de manuel olarak işaretleyebilir
  - **Araç ekleme / düzenleme / silme** (plaka tekilliği kontrolü, kullanımda olan araç silinemez)
  - **Yönetici (admin) hesabı ekleme / silme** (ana yönetici hesabı silinemez, kendi hesabınızı silemezsiniz)
- ✅ Normal (giriş yapmayan) kullanıcıların yetkisi **sadece form doldurma ve kendi formunu sorgulama/dönüş bildirme**
  ile sınırlıdır — onay, araç/yönetici yönetimi gibi işlemler sadece admin girişiyle yapılabilir.
- ✅ Yazdırılabilir görev formu detay sayfası (admin panelinde "Yazdır" butonu).
- ✅ Türkçe arayüz, responsive (mobil uyumlu) Bootstrap tasarım.

## Teknik Mimari ve Önemli Not

Bu proje **.NET 8 / ASP.NET Core MVC** ile yazılmıştır. Geliştirme ortamında NuGet.org erişimi kısıtlı
olduğu için (kurumsal/sanal ortam kısıtlaması), veri katmanı **Entity Framework Core yerine .NET'in
kendi yerleşik bileşenleriyle** (System.Text.Json + basit bir dosya tabanlı repository deseni)
yazılmıştır. Kimlik doğrulama da ekstra paket gerektirmeyen **yerleşik Cookie Authentication** ile
yapılmaktadır. Bu sayede proje **hiçbir NuGet paketi indirmeden, olduğu gibi derlenip çalışır.**

Veriler `App_Data` klasöründe JSON dosyaları olarak saklanır:
- `App_Data/vehicles.json` — araçlar
- `App_Data/forms.json` — görev formları
- `App_Data/admins.json` — yönetici hesapları

**Gerçek bir SQL Server / EF Core'a geçiş** isterseniz, `Data/` klasöründeki repository sınıflarının
(`VehicleRepository`, `GorevFormuRepository`, `AdminUserRepository`) iç implementasyonunu EF Core
`DbContext` kullanacak şekilde değiştirmeniz yeterlidir — controller'lar ve view'lar hiç değişmeden
çalışmaya devam eder, çünkü onlar sadece repository sınıflarını kullanır (interface benzeri kullanım).
İnternet erişimi olan bir makinede şu paketleri ekleyerek EF Core + SQL Server'a geçebilirsiniz:
`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`,
`Microsoft.AspNetCore.Identity.EntityFrameworkCore` (isterseniz tam Identity altyapısı için).

## Klasör Yapısı

```
AracGorevFormu/
├── Controllers/
│   ├── HomeController.cs      -> ana sayfa yönlendirmesi
│   ├── FormController.cs      -> ANONİM: form doldurma, sorgulama, dönüş bildirme
│   ├── AccountController.cs   -> admin giriş/çıkış
│   └── AdminController.cs     -> [Authorize] admin paneli, onay, araç/yönetici CRUD
├── Models/
│   ├── Vehicle.cs, GorevFormu.cs, GorevDurumu.cs, AdminUser.cs
│   └── ViewModels/FormViewModels.cs
├── Data/
│   ├── JsonFileStore.cs               -> generic thread-safe JSON dosya deposu
│   ├── VehicleRepository.cs
│   ├── GorevFormuRepository.cs
│   ├── AdminUserRepository.cs
│   └── SeedData.cs                    -> ilk açılışta örnek veri + admin hesabı oluşturur
├── Services/PasswordHasher.cs         -> PBKDF2 ile şifre hashleme (yerleşik .NET API)
├── Views/                             -> Form, Account, Admin, Shared klasörleri
├── wwwroot/                           -> Bootstrap, jQuery (proje içinde hazır, internet gerekmez)
└── App_Data/                          -> JSON veri dosyaları (ilk çalıştırmada otomatik oluşur)
```

## Çalıştırma

Gereksinim: **.NET 8 SDK**

```bash
cd AracGorevFormu
dotnet restore
dotnet run
```

Uygulama varsayılan olarak `http://localhost:5000` (veya `Properties/launchSettings.json`'da
tanımlı port) üzerinden ayağa kalkar. Tarayıcıdan açtığınızda ana sayfa otomatik olarak
"Görev Formu Doldur" sayfasına yönlenir.

### Varsayılan Admin Hesabı

İlk çalıştırmada otomatik oluşturulur:

| Kullanıcı Adı | Şifre       |
|---------------|-------------|
| `admin`       | `Admin123!` |

Güvenlik için ilk girişten sonra yönetim panelinden yeni bir admin hesabı oluşturup
bu varsayılan hesabı kullanmamanızı öneririz (ana yönetici hesabı silinemediği için,
şifresini değiştirmek isterseniz `Kullanicilar` ekranından yeni bir hesap ekleyip
onu kullanmaya başlayabilirsiniz; şifre değiştirme ekranı sonraki bir geliştirme
adımı olarak eklenebilir).

### Örnek Kullanım Akışı

1. **Personel** → "Görev Formu Doldur" → araç seçer, bilgileri girer → gönderir → bir **takip kodu** alır.
2. **Admin** → giriş yapar → "Görev Formları" ekranından bekleyen formu görür → **Onaylar** ya da **Reddeder**.
3. Onaylanan form için araç durumu "Onaylandı - Araç Dışarıda" olur ve admin dashboard'unda
   "Şu An Dışarıda Olan Araçlar" listesinde görünür.
4. Personel, işi bitince aldığı takip kodunu "Durum Sorgula" ekranına girer ve
   **"Aracı Teslim Ettim / Döndü"** butonuna basarak dönüşü bildirir (admin de isterse manuel işaretleyebilir).
5. Form durumu "Tamamlandı - Araç Teslim Edildi" olur, araç tekrar müsait duruma geçer.

## Genişletme Fikirleri (opsiyonel, ileri seviye)

- E-posta/SMS bildirimi (form onaylandığında/reddedildiğinde otomatik bilgilendirme)
- Araç bazlı kullanım geçmişi ve km/yakıt takibi
- Tarih aralığı bazlı raporlama ve Excel'e aktarma
- Gerçek veritabanına (SQL Server/PostgreSQL) EF Core ile geçiş
- Şifre değiştirme / şifremi unuttum akışı
