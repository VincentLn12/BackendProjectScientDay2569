using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddAdvisorCountToCompetition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(name: "AdvisorCount", table: "Competitions", type: "int", nullable: false, defaultValue: 1);
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "AdvisorCount", table: "Competitions");
    }
}
