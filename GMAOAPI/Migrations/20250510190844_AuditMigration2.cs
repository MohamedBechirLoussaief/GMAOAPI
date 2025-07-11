using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GMAOAPI.Migrations
{
    /// <inheritdoc />
    public partial class AuditMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Audits");

            migrationBuilder.AddColumn<string>(
                name: "UtilisateurId",
                table: "Audits",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Audits_UtilisateurId",
                table: "Audits",
                column: "UtilisateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audits_AspNetUsers_UtilisateurId",
                table: "Audits",
                column: "UtilisateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audits_AspNetUsers_UtilisateurId",
                table: "Audits");

            migrationBuilder.DropIndex(
                name: "IX_Audits_UtilisateurId",
                table: "Audits");

            migrationBuilder.DropColumn(
                name: "UtilisateurId",
                table: "Audits");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Audits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
