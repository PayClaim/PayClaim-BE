using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayClaim.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationToTripProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "Trip_proposals",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destination",
                table: "Trip_proposals");
        }
    }
}
