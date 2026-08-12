using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class teamteacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamManagerPrimary",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamManagerSecondary",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryFirstName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryLastName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryTitlePrefix",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryFirstName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryLastName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondaryTitlePrefix",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryFirstName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "PrimaryLastName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "PrimaryTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SecondaryFirstName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SecondaryLastName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SecondaryTitlePrefix",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "TeamManagerPrimary",
                table: "Teams",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamManagerSecondary",
                table: "Teams",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
