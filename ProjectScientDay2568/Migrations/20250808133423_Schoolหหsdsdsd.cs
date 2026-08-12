using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class Schoolหหsdsdsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_schools_SchoolCode",
                table: "Teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_schools",
                table: "schools");

            migrationBuilder.RenameTable(
                name: "schools",
                newName: "School");

            migrationBuilder.AddPrimaryKey(
                name: "PK_School",
                table: "School",
                column: "school_code");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_School_SchoolCode",
                table: "Teams",
                column: "SchoolCode",
                principalTable: "School",
                principalColumn: "school_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_School_SchoolCode",
                table: "Teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_School",
                table: "School");

            migrationBuilder.RenameTable(
                name: "School",
                newName: "schools");

            migrationBuilder.AddPrimaryKey(
                name: "PK_schools",
                table: "schools",
                column: "school_code");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_schools_SchoolCode",
                table: "Teams",
                column: "SchoolCode",
                principalTable: "schools",
                principalColumn: "school_code");
        }
    }
}
