using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GMAOAPI.Migrations
{
    /// <inheritdoc />
    public partial class equipementDesactivationArchivagelOGIC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Resultat",
                table: "Rapports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArchiveReason",
                table: "Planifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnnulationReason",
                table: "Interventions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArchiveReason",
                table: "Interventions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resultat",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Planifications");

            migrationBuilder.DropColumn(
                name: "AnnulationReason",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Interventions");
        }
    }
}
