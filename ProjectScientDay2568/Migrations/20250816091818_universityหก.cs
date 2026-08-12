using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class universityหก : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Awards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Awards",
                columns: table => new
                {
                    AwardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetitionLevelID = table.Column<int>(type: "int", nullable: false),
                    RewardTypeID = table.Column<int>(type: "int", nullable: false),
                    AwardName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HasCertificate = table.Column<bool>(type: "bit", nullable: false),
                    PrizeMoney = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.AwardID);
                    table.ForeignKey(
                        name: "FK_Awards_CompetitionLevels_CompetitionLevelID",
                        column: x => x.CompetitionLevelID,
                        principalTable: "CompetitionLevels",
                        principalColumn: "CompetitionLevelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Awards_RewardTypes_RewardTypeID",
                        column: x => x.RewardTypeID,
                        principalTable: "RewardTypes",
                        principalColumn: "RewardTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Awards_CompetitionLevelID",
                table: "Awards",
                column: "CompetitionLevelID");

            migrationBuilder.CreateIndex(
                name: "IX_Awards_RewardTypeID",
                table: "Awards",
                column: "RewardTypeID");
        }
    }
}
