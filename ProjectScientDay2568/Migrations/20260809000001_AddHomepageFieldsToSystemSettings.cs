using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    public partial class AddHomepageFieldsToSystemSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "HomeHeroTitle", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeHeroHighlight", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeHeroDescription", table: "SystemSettings", type: "nvarchar(2000)", maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeRegisterButtonLabel", table: "SystemSettings", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeGuideButtonLabel", table: "SystemSettings", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeApplicantsButtonLabel", table: "SystemSettings", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "ShowHomeNews", table: "SystemSettings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "HomeNewsTitle", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeNewsDescription", table: "SystemSettings", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HomeNewsButtonLabel", table: "SystemSettings", type: "nvarchar(100)", maxLength: 100, nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var name in new[] { "HomeHeroTitle", "HomeHeroHighlight", "HomeHeroDescription", "HomeRegisterButtonLabel", "HomeGuideButtonLabel", "HomeApplicantsButtonLabel", "ShowHomeNews", "HomeNewsTitle", "HomeNewsDescription", "HomeNewsButtonLabel" })
                migrationBuilder.DropColumn(name: name, table: "SystemSettings");
        }
    }
}
