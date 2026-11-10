using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DiscardOrphanPendingAiSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy orphan cleanup: the confirm-first flow removes the
            // pending-review queue, so AI drafts left unreviewed become
            // inactive and explicitly not approved.
            migrationBuilder.Sql("UPDATE Documents SET Approved = 0, Active = 0 WHERE GeneratedByAI = 1 AND Active = 1 AND Approved IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
