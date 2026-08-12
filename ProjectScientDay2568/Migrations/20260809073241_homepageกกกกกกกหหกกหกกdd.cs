using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class homepageกกกกกกกหหกกหกกdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvisorCount",
                table: "Competitions");

            migrationBuilder.AddColumn<int>(
                name: "AdvisorCount",
                table: "CompetitionYears",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvisorCount",
                table: "CompetitionYears");

            migrationBuilder.AddColumn<int>(
                name: "AdvisorCount",
                table: "Competitions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
