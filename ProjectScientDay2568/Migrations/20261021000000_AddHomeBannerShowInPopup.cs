using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    // เขียนเอง (ไม่ใช้ dotnet ef migrations add) เพราะ ApplicationDbContextModelSnapshot เป็น stub ว่าง
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261021000000_AddHomeBannerShowInPopup")]
    public partial class AddHomeBannerShowInPopup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowInPopup",
                table: "HomeBanners",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInPopup",
                table: "HomeBanners");
        }
    }
}
