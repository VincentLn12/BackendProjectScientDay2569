using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class UniversityDeanNamess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateBackgroundPath",
                table: "Years");

            migrationBuilder.AddColumn<string>(
                name: "CertificateBackgroundPath",
                table: "CertificateConfigs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateBackgroundPath",
                table: "CertificateConfigs");

            migrationBuilder.AddColumn<string>(
                name: "CertificateBackgroundPath",
                table: "Years",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
