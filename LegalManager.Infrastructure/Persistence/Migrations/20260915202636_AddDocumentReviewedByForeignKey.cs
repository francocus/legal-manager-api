using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviewedByForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_UploadedByUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents");

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

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
