using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayClaim.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardAndApprovalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claim_Approvals_ClaimId",
                table: "Claim_Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_proposals_Status_SubmittedAt",
                table: "Trip_proposals",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claim_Approvals_ClaimId_ApprovalLevel",
                table: "Claim_Approvals",
                columns: new[] { "ClaimId", "ApprovalLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Claim_Approvals_ClaimId_ApproverId",
                table: "Claim_Approvals",
                columns: new[] { "ClaimId", "ApproverId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trip_proposals_Status_SubmittedAt",
                table: "Trip_proposals");

            migrationBuilder.DropIndex(
                name: "IX_Claim_Approvals_ClaimId_ApprovalLevel",
                table: "Claim_Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Claim_Approvals_ClaimId_ApproverId",
                table: "Claim_Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Claim_Approvals_ClaimId",
                table: "Claim_Approvals",
                column: "ClaimId");
        }
    }
}
