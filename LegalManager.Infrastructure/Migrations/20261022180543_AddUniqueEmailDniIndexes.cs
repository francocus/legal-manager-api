using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmailDniIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Lawyers_Dni",
                table: "Lawyers",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Lawyers_Email",
                table: "Lawyers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Clients_Dni",
                table: "Clients",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Clients_Email",
                table: "Clients",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Admins_Dni",
                table: "Admins",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Lawyers_Dni",
                table: "Lawyers");

            migrationBuilder.DropIndex(
                name: "UX_Lawyers_Email",
                table: "Lawyers");

            migrationBuilder.DropIndex(
                name: "UX_Clients_Dni",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "UX_Clients_Email",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "UX_Admins_Dni",
                table: "Admins");

            migrationBuilder.DropIndex(
                name: "UX_Admins_Email",
                table: "Admins");
        }
    }
}
