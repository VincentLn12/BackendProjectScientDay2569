using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddTeamAdvisors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "AdvisorCount", table: "CompetitionLevels", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.CreateTable(name: "TeamAdvisors", columns: table => new { TeamAdvisorID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), TeamID = table.Column<int>(type: "int", nullable: false), SortOrder = table.Column<int>(type: "int", nullable: false), TitlePrefix = table.Column<int>(type: "int", nullable: false), OtherTitlePrefix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true), FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false) }, constraints: table => { table.PrimaryKey("PK_TeamAdvisors", x => x.TeamAdvisorID); table.ForeignKey("FK_TeamAdvisors_Teams_TeamID", x => x.TeamID, "Teams", "TeamID", onDelete: ReferentialAction.Cascade); });
            migrationBuilder.CreateIndex(name: "IX_TeamAdvisors_TeamID", table: "TeamAdvisors", column: "TeamID");
        }
        protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "TeamAdvisors"); migrationBuilder.DropColumn(name: "AdvisorCount", table: "CompetitionLevels"); }
    }
}
