using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayClaim.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claims_EmployeeId",
                table: "Claims");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EmployeeId_SubmittedAt",
                table: "Claims",
                columns: new[] { "EmployeeId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status_SubmittedAt",
                table: "Claims",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_SubmittedAt",
                table: "Claims",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_TransactionDate",
                table: "Claims",
                column: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claims_EmployeeId_SubmittedAt",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_Status",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_Status_SubmittedAt",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_SubmittedAt",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_TransactionDate",
                table: "Claims");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_EmployeeId",
                table: "Claims",
                column: "EmployeeId");
        }
    }
}
