using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    // เขียนเอง (ไม่ใช้ dotnet ef migrations add) เพราะ ApplicationDbContextModelSnapshot เป็น stub ว่าง
    // ย้ายกลุ่มไลน์จากกลุ่มรวมใน SystemSettings ไปเป็นของแต่ละหลักสูตร (ไม่ย้ายข้อมูลเดิม)
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261022110000_MoveTrainingLineToTopics")]
    public partial class MoveTrainingLineToTopics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "LineGroupUrl", table: "Trainingtopics", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "LineQrPath", table: "Trainingtopics", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "LineNote", table: "Trainingtopics", type: "nvarchar(500)", maxLength: 500, nullable: true);

            migrationBuilder.DropColumn(name: "TrainingLineQrPath", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "TrainingLineUrl", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "TrainingLineNote", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "TrainingLinePopupEnabled", table: "SystemSettings");
            migrationBuilder.DropColumn(name: "TrainingLineUpdatedAt", table: "SystemSettings");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LineGroupUrl", table: "Trainingtopics");
            migrationBuilder.DropColumn(name: "LineQrPath", table: "Trainingtopics");
            migrationBuilder.DropColumn(name: "LineNote", table: "Trainingtopics");

            migrationBuilder.AddColumn<string>(name: "TrainingLineQrPath", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TrainingLineUrl", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TrainingLineNote", table: "SystemSettings", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "TrainingLinePopupEnabled", table: "SystemSettings", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<DateTime>(name: "TrainingLineUpdatedAt", table: "SystemSettings", type: "datetime2", nullable: true);
        }
    }
}
