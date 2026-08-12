using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class homeconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YearID = table.Column<int>(type: "int", nullable: false),
                    HeroBadge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroSubtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalRegisterUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowQuickActionBar = table.Column<bool>(type: "bit", nullable: false),
                    HeroBackgroundPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeConfigs_Years_YearID",
                        column: x => x.YearID,
                        principalTable: "Years",
                        principalColumn: "YearID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeConfigs_YearID",
                table: "HomeConfigs",
                column: "YearID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeConfigs");
        }
    }
}
