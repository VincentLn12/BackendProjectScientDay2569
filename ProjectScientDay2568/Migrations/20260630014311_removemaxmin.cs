using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class removemaxmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTeamMembers",
                table: "CompetitionLevels");

            migrationBuilder.DropColumn(
                name: "MinTeamMembers",
                table: "CompetitionLevels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTeamMembers",
                table: "CompetitionLevels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinTeamMembers",
                table: "CompetitionLevels",
                type: "int",
                nullable: true);
        }
    }
}
