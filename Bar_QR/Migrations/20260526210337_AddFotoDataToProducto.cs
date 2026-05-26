using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bar_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoDataToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FotoData",
                table: "Productos",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoMimeType",
                table: "Productos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoData",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "FotoMimeType",
                table: "Productos");
        }
    }
}
