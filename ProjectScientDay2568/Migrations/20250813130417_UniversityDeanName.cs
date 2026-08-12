using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class UniversityDeanName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacultyDeanName",
                table: "CertificateConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacultyDeanSignature",
                table: "CertificateConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityDeanName",
                table: "CertificateConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityDeanSignature",
                table: "CertificateConfigs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacultyDeanName",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "FacultyDeanSignature",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "UniversityDeanName",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "UniversityDeanSignature",
                table: "CertificateConfigs");
        }
    }
}
