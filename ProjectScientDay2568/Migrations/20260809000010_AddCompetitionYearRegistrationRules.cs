using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddCompetitionYearRegistrationRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "RegistrationMode", table: "CompetitionYears", type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "team");
            migrationBuilder.AddColumn<int>(name: "MaxStudentsPerSchool", table: "CompetitionYears", type: "int", nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RegistrationMode", table: "CompetitionYears");
            migrationBuilder.DropColumn(name: "MaxStudentsPerSchool", table: "CompetitionYears");
        }
    }
}
