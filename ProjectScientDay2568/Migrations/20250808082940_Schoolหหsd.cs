using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class Schoolหหsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolCode",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SchoolCode",
                table: "Teams",
                column: "SchoolCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_schools_SchoolCode",
                table: "Teams",
                column: "SchoolCode",
                principalTable: "schools",
                principalColumn: "school_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_schools_SchoolCode",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_SchoolCode",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SchoolCode",
                table: "Teams");
        }
    }
}
