using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentApprovedAndFixUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Documents",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReviewedByUserId",
                table: "Documents",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ReviewedByUserId",
                table: "Documents",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ReviewedByUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ReviewedByUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Documents");
        }
    }
}
