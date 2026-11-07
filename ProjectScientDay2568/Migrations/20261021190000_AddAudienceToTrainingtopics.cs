using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    // เขียนเอง (ไม่ใช้ dotnet ef migrations add) เพราะ ApplicationDbContextModelSnapshot เป็น stub ว่าง
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261021190000_AddAudienceToTrainingtopics")]
    public partial class AddAudienceToTrainingtopics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audience",
                table: "Trainingtopics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Audience", table: "Trainingtopics");
        }
    }
}
