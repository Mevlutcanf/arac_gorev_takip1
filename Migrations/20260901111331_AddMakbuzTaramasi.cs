using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AracGorevFormu.Migrations
{
    /// <inheritdoc />
    public partial class AddMakbuzTaramasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MakbuzDosyaYolu",
                table: "MakineBakimlari",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MakbuzMetni",
                table: "MakineBakimlari",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MakbuzDosyaYolu",
                table: "MakineBakimlari");

            migrationBuilder.DropColumn(
                name: "MakbuzMetni",
                table: "MakineBakimlari");
        }
    }
}
