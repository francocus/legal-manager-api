using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeactivatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Lawyers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Documents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Clients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Cases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Appointments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeactivatedAt",
                table: "Admins",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Lawyers");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Admins");
        }
    }
}
