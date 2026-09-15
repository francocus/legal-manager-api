using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCaseLawyerLawyerIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseLawyer_Cases_CaseId",
                table: "CaseLawyer");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseLawyer_Lawyers_lawyersId",
                table: "CaseLawyer");

            migrationBuilder.RenameColumn(
                name: "lawyersId",
                table: "CaseLawyer",
                newName: "LawyerId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseLawyer_lawyersId",
                table: "CaseLawyer",
                newName: "IX_CaseLawyer_LawyerId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseLawyer_Cases_CaseId",
                table: "CaseLawyer",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseLawyer_Lawyers_LawyerId",
                table: "CaseLawyer",
                column: "LawyerId",
                principalTable: "Lawyers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseLawyer_Cases_CaseId",
                table: "CaseLawyer");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseLawyer_Lawyers_LawyerId",
                table: "CaseLawyer");

            migrationBuilder.RenameColumn(
                name: "LawyerId",
                table: "CaseLawyer",
                newName: "lawyersId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseLawyer_LawyerId",
                table: "CaseLawyer",
                newName: "IX_CaseLawyer_lawyersId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseLawyer_Cases_CaseId",
                table: "CaseLawyer",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseLawyer_Lawyers_lawyersId",
                table: "CaseLawyer",
                column: "lawyersId",
                principalTable: "Lawyers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
