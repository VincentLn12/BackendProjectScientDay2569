using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class sdasdbbdffhหฟก : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCheckedIn",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Students_RewardTypeID",
                table: "Students",
                column: "RewardTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_RewardTypes_RewardTypeID",
                table: "Students",
                column: "RewardTypeID",
                principalTable: "RewardTypes",
                principalColumn: "RewardTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_RewardTypes_RewardTypeID",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_RewardTypeID",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsCheckedIn",
                table: "Students");
        }
    }
}
