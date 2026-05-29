using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bar_QR.Migrations
{
    /// <inheritdoc />
    public partial class EmpleadosYZonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Alto",
                table: "Mesas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ancho",
                table: "Mesas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosX",
                table: "Mesas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosY",
                table: "Mesas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZonaId",
                table: "Mesas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarTipo = table.Column<string>(type: "TEXT", nullable: false),
                    FotoData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    FotoMime = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zonas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zonas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_ZonaId",
                table: "Mesas",
                column: "ZonaId");

            // SQLite no soporta AddForeignKey en tablas existentes; la relación se gestiona por EF Core.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Zonas");

            migrationBuilder.DropIndex(
                name: "IX_Mesas_ZonaId",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Alto",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Ancho",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "PosX",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "PosY",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "ZonaId",
                table: "Mesas");
        }
    }
}
