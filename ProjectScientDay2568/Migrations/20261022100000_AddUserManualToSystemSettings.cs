using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    // เขียนเอง (ไม่ใช้ dotnet ef migrations add) เพราะ ApplicationDbContextModelSnapshot เป็น stub ว่าง
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261022100000_AddUserManualToSystemSettings")]
    public partial class AddUserManualToSystemSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserManualPath",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserManualFileName",
                table: "SystemSettings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UserManualUpdatedAt",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UserManualPath", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "UserManualFileName", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "UserManualUpdatedAt", table: "SystemSettings");
        }
    }
}
