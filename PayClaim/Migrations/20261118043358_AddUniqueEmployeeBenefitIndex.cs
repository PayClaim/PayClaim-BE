using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayClaim.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmployeeBenefitIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bersihkan baris duplikat (EmployeeId, BenefitId) sebelum membuat unique index.
            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT Id,
                           FIRST_VALUE(Id) OVER (PARTITION BY EmployeeId, BenefitId ORDER BY EndDate DESC, Id DESC) AS KeepId
                    FROM Employee_Benefit
                )
                UPDATE c
                SET c.EmployeeBenefitId = r.KeepId
                FROM Claims c
                INNER JOIN Ranked r ON c.EmployeeBenefitId = r.Id
                WHERE r.Id <> r.KeepId;

                ;WITH Duplicates AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY EmployeeId, BenefitId ORDER BY EndDate DESC, Id DESC) AS rn
                    FROM Employee_Benefit
                )
                DELETE FROM Duplicates WHERE rn > 1;
            ");

            migrationBuilder.DropIndex(
                name: "IX_Employee_Benefit_EmployeeId",
                table: "Employee_Benefit");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Benefit_EmployeeId_BenefitId",
                table: "Employee_Benefit",
                columns: new[] { "EmployeeId", "BenefitId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employee_Benefit_EmployeeId_BenefitId",
                table: "Employee_Benefit");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Benefit_EmployeeId",
                table: "Employee_Benefit",
                column: "EmployeeId");
        }
    }
}
