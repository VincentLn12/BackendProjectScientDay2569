using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260811000000_RemoveApplicationDeadline")]
    public partial class RemoveApplicationDeadline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationDeadline",
                table: "CompetitionYears");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationDeadline",
                table: "CompetitionYears",
                type: "datetime2",
                nullable: true);
        }
    }
}
