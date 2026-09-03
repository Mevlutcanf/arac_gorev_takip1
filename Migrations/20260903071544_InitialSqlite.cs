using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AracGorevFormu.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciAdi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AdSoyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnaYonetici = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AracBakimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Plaka = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BakimTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BakimTuru = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Km = table.Column<int>(type: "INTEGER", nullable: false),
                    YapilanIslemler = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ServisAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Maliyet = table.Column<decimal>(type: "TEXT", nullable: false),
                    SonrakiBakimTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SonrakiBakimKm = table.Column<int>(type: "INTEGER", nullable: true),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracBakimlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArventoAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiUrl = table.Column<string>(type: "TEXT", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", nullable: true),
                    Sifre = table.Column<string>(type: "TEXT", nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArventoAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DosyaEkleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentTuru = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: false),
                    DosyaAdi = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DosyaTipi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Icerik = table.Column<byte[]>(type: "BLOB", nullable: false),
                    YuklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DosyaEkleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GorevFormlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TakipKodu = table.Column<string>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                    AracPlaka = table.Column<string>(type: "TEXT", nullable: false),
                    AracMarka = table.Column<string>(type: "TEXT", nullable: false),
                    AracModel = table.Column<string>(type: "TEXT", nullable: false),
                    KullananAdSoyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KullananTelefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Departman = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    GorevAmaci = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CikisZamani = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlanlananDonusZamani = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GercekDonusZamani = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CikisKm = table.Column<int>(type: "INTEGER", nullable: true),
                    DonusKm = table.Column<int>(type: "INTEGER", nullable: true),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OnaylayanKullaniciAdi = table.Column<string>(type: "TEXT", nullable: true),
                    OnayTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RedNedeni = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevFormlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HgsGecisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Plaka = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GecisTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GiseAdı = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tutar = table.Column<decimal>(type: "TEXT", nullable: false),
                    OdediMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    CezaMi = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HgsGecisleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Makineler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Marka = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SeriNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Lokasyon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalismaSaati = table.Column<int>(type: "INTEGER", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Makineler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmtpAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SmtpServer = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    SenderEmail = table.Column<string>(type: "TEXT", nullable: false),
                    SenderPassword = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationEmails = table.Column<string>(type: "TEXT", nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IslemTuru = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Detay = table.Column<string>(type: "TEXT", nullable: false),
                    IpAdresi = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Plaka = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Marka = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Renk = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    SahiplikTuru = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SabitSurucu = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Lokasyon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SasiNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MotorNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TescilTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MuayeneBitisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SigortaBitisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RuhsatDosyaYolu = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    RuhsatDosyaIcerigi = table.Column<byte[]>(type: "BLOB", nullable: true),
                    RuhsatDosyaAdi = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    RuhsatDosyaTipi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GuncelKm = table.Column<int>(type: "INTEGER", nullable: true),
                    SonKonumZamani = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SonAdres = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MakineBakimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakineId = table.Column<int>(type: "INTEGER", nullable: false),
                    BakimTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BakimTuru = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CalismaSaati = table.Column<int>(type: "INTEGER", nullable: false),
                    YapilanIslemler = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DegisenParcalar = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    BakimiYapan = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Maliyet = table.Column<decimal>(type: "TEXT", nullable: false),
                    SonrakiBakimTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SonrakiBakimCalismaSaati = table.Column<int>(type: "INTEGER", nullable: true),
                    MakbuzDosyaYolu = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    MakbuzMetni = table.Column<string>(type: "TEXT", nullable: true),
                    EklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
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
