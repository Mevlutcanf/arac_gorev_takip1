using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AracGorevFormu.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnaYonetici = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AracBakimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Plaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BakimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BakimTuru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Km = table.Column<int>(type: "int", nullable: false),
                    YapilanIslemler = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ServisAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Maliyet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SonrakiBakimTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonrakiBakimKm = table.Column<int>(type: "int", nullable: true),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracBakimlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArventoAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sifre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArventoAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DosyaEkleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentTuru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    DosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DosyaTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icerik = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    YuklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DosyaEkleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GorevFormlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakipKodu = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    AracPlaka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AracMarka = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AracModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KullananAdSoyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KullananTelefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Departman = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GorevAmaci = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CikisZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanlananDonusZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GercekDonusZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CikisKm = table.Column<int>(type: "int", nullable: true),
                    DonusKm = table.Column<int>(type: "int", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnaylayanKullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedNedeni = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevFormlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HgsGecisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Plaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GecisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GiseAdı = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OdediMi = table.Column<bool>(type: "bit", nullable: false),
                    CezaMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HgsGecisleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Makineler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeriNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Lokasyon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    CalismaSaati = table.Column<int>(type: "int", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Makineler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmtpAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpServer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationEmails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IslemTuru = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAdresi = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Plaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Renk = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SahiplikTuru = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SabitSurucu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Lokasyon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SasiNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MotorNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TescilTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MuayeneBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SigortaBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RuhsatDosyaYolu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RuhsatDosyaIcerigi = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    RuhsatDosyaAdi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RuhsatDosyaTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuncelKm = table.Column<int>(type: "int", nullable: true),
                    SonKonumZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonAdres = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MakineBakimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MakineId = table.Column<int>(type: "int", nullable: false),
                    BakimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BakimTuru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CalismaSaati = table.Column<int>(type: "int", nullable: false),
                    YapilanIslemler = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DegisenParcalar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BakimiYapan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Maliyet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SonrakiBakimTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonrakiBakimCalismaSaati = table.Column<int>(type: "int", nullable: true),
                    MakbuzDosyaYolu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MakbuzMetni = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MakineBakimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MakineBakimlari_Makineler_MakineId",
                        column: x => x.MakineId,
                        principalTable: "Makineler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GorevFormlari_TakipKodu",
                table: "GorevFormlari",
                column: "TakipKodu");

            migrationBuilder.CreateIndex(
                name: "IX_MakineBakimlari_MakineId",
                table: "MakineBakimlari",
                column: "MakineId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Plaka",
                table: "Vehicles",
                column: "Plaka");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "AracBakimlari");

            migrationBuilder.DropTable(
                name: "ArventoAyarlari");

            migrationBuilder.DropTable(
                name: "DosyaEkleri");

            migrationBuilder.DropTable(
                name: "GorevFormlari");

            migrationBuilder.DropTable(
                name: "HgsGecisleri");

            migrationBuilder.DropTable(
                name: "MakineBakimlari");

            migrationBuilder.DropTable(
                name: "SmtpAyarlari");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Makineler");
        }
    }
}
