using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bar_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddMesaNombreSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Mesas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Mesas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_Slug",
                table: "Mesas",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mesas_Slug",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Mesas");
        }
    }
}
