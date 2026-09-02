using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AracGorevFormu.Migrations
{
    public partial class AddMakineModulu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_MakineBakimlari_MakineId",
                table: "MakineBakimlari",
                column: "MakineId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MakineBakimlari");

            migrationBuilder.DropTable(
                name: "Makineler");
        }
    }
}
