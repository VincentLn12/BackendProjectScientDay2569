using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class Schoolหหsdsdsdsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_School_SchoolCode",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "SchoolCode",
                table: "Teams",
                newName: "school_code");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_SchoolCode",
                table: "Teams",
                newName: "IX_Teams_school_code");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_School_school_code",
                table: "Teams",
                column: "school_code",
                principalTable: "School",
                principalColumn: "school_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_School_school_code",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "school_code",
                table: "Teams",
                newName: "SchoolCode");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_school_code",
                table: "Teams",
                newName: "IX_Teams_SchoolCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_School_SchoolCode",
                table: "Teams",
                column: "SchoolCode",
                principalTable: "School",
                principalColumn: "school_code");
        }
    }
}
