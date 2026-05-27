using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bar_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPlantilla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketImagenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    Zona = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketImagenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketPlantillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImprimirHora = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImprimirUsuario = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImprimirImpuestos = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImprimirDesglose = table.Column<bool>(type: "INTEGER", nullable: false),
                    CabeceraJson = table.Column<string>(type: "TEXT", nullable: false),
                    PieJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPlantillas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketImagenes");

            migrationBuilder.DropTable(
                name: "TicketPlantillas");
        }
    }
}
