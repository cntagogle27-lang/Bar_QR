using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bar_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddGrupoAndFotoToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Categoria",
                table: "Productos",
                newName: "Grupo");

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Productos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Productos");

            migrationBuilder.RenameColumn(
                name: "Grupo",
                table: "Productos",
                newName: "Categoria");
        }
    }
}
