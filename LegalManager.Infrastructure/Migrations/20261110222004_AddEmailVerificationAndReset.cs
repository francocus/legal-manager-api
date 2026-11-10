using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Lawyers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Lawyers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Lawyers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Lawyers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Lawyers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Clients",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Clients",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Admins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Admins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Admins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Admins",
                type: "datetime2",
                nullable: true);

            // Existing accounts predate the email flow and must not get locked out of
            // login: they all count as confirmed. Not in Down (reverting does not
            // reconstruct which rows were confirmed before).
            migrationBuilder.Sql("UPDATE Clients SET EmailConfirmed = 1; UPDATE Lawyers SET EmailConfirmed = 1; UPDATE Admins SET EmailConfirmed = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Admins");
        }
    }
}
