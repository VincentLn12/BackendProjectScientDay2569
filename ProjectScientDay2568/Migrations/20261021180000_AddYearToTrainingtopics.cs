using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectScientDay2568.Data;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    // เขียนเอง (ไม่ใช้ dotnet ef migrations add) เพราะ ApplicationDbContextModelSnapshot เป็น stub ว่าง
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261021180000_AddYearToTrainingtopics")]
    public partial class AddYearToTrainingtopics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearID",
                table: "Trainingtopics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trainingtopics_YearID",
                table: "Trainingtopics",
                column: "YearID");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainingtopics_Years_YearID",
                table: "Trainingtopics",
                column: "YearID",
                principalTable: "Years",
                principalColumn: "YearID",
                onDelete: ReferentialAction.SetNull);

            // ข้อมูลเดิม: เติมปีจากวันอบรม (ค.ศ. + 543 = พ.ศ.) ถ้าไม่มีวันอบรมใช้วันที่สร้าง
            migrationBuilder.Sql(@"
UPDATE t SET t.YearID = y.YearID
FROM Trainingtopics t
JOIN Years y ON y.YearNumber = YEAR(COALESCE(t.TrainingDate, t.CreatedAt)) + 543
WHERE t.YearID IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Trainingtopics_Years_YearID", table: "Trainingtopics");
            migrationBuilder.DropIndex(name: "IX_Trainingtopics_YearID", table: "Trainingtopics");
            migrationBuilder.DropColumn(name: "YearID", table: "Trainingtopics");
        }
    }
}
