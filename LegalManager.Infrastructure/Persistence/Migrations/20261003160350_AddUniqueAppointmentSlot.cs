using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueAppointmentSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_LawyerId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_Lawyer_Slot",
                table: "Appointments",
                columns: new[] { "LawyerId", "Date", "Time" },
                unique: true,
                filter: "[Active] = 1 AND [Status] <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Appointments_Lawyer_Slot",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LawyerId",
                table: "Appointments",
                column: "LawyerId");
        }
    }
}
