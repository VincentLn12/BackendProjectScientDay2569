using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class homepage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeApplicantsButtonLabel",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeGuideButtonLabel",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeHeroDescription",
                table: "SystemSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeHeroHighlight",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeHeroTitle",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeNewsButtonLabel",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeNewsDescription",
                table: "SystemSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeNewsTitle",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeRegisterButtonLabel",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomepageContent",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHomeNews",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeApplicantsButtonLabel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeGuideButtonLabel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeHeroDescription",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeHeroHighlight",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeHeroTitle",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeNewsButtonLabel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeNewsDescription",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeNewsTitle",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomeRegisterButtonLabel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "HomepageContent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ShowHomeNews",
                table: "SystemSettings");
        }
    }
}
