using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddThirdToFifthTeamAdvisors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var prefix in new[] { "Third", "Fourth", "Fifth" })
            {
                migrationBuilder.AddColumn<int>(name: $"{prefix}TitlePrefix", table: "Teams", type: "int", nullable: false, defaultValue: 0);
                migrationBuilder.AddColumn<string>(name: $"{prefix}OtherTitlePrefix", table: "Teams", type: "nvarchar(100)", maxLength: 100, nullable: true);
                migrationBuilder.AddColumn<string>(name: $"{prefix}FirstName", table: "Teams", type: "nvarchar(100)", maxLength: 100, nullable: true);
                migrationBuilder.AddColumn<string>(name: $"{prefix}LastName", table: "Teams", type: "nvarchar(100)", maxLength: 100, nullable: true);
            }
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var prefix in new[] { "Third", "Fourth", "Fifth" }) foreach (var suffix in new[] { "TitlePrefix", "OtherTitlePrefix", "FirstName", "LastName" }) migrationBuilder.DropColumn(name: $"{prefix}{suffix}", table: "Teams");
        }
    }
}
