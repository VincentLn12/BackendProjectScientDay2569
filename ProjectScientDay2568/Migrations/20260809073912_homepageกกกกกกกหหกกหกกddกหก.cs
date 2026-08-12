using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class homepageกกกกกกกหหกกหกกddกหก : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FifthFirstName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FifthLastName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FifthOtherTitlePrefix",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FifthTitlePrefix",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FourthFirstName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FourthLastName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FourthOtherTitlePrefix",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FourthTitlePrefix",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThirdFirstName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThirdLastName",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThirdOtherTitlePrefix",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThirdTitlePrefix",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FifthFirstName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FifthLastName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FifthOtherTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FifthTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FourthFirstName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FourthLastName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FourthOtherTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FourthTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ThirdFirstName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ThirdLastName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ThirdOtherTitlePrefix",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ThirdTitlePrefix",
                table: "Teams");
        }
    }
}
