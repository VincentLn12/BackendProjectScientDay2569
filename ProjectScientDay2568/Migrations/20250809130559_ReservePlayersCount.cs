using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class ReservePlayersCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MainPlayersCount",
                table: "CompetitionLevels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservePlayersCount",
                table: "CompetitionLevels",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainPlayersCount",
                table: "CompetitionLevels");

            migrationBuilder.DropColumn(
                name: "ReservePlayersCount",
                table: "CompetitionLevels");
        }
    }
}
