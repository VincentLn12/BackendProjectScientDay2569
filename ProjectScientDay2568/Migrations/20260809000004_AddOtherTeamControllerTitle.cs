using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddOtherTeamControllerTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "PrimaryOtherTitlePrefix", table: "Teams", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SecondaryOtherTitlePrefix", table: "Teams", type: "nvarchar(100)", maxLength: 100, nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PrimaryOtherTitlePrefix", table: "Teams");
            migrationBuilder.DropColumn(name: "SecondaryOtherTitlePrefix", table: "Teams");
        }
    }
}
