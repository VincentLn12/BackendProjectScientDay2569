using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class RemoveAdvisorCountFromCompetitionLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "AdvisorCount", table: "CompetitionLevels");
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(name: "AdvisorCount", table: "CompetitionLevels", type: "int", nullable: false, defaultValue: 1);
    }
}
