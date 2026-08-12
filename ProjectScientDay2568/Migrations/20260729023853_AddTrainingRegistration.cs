using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScientDay2568.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trainingtopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrainingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    IsOpenRegister = table.Column<bool>(type: "bit", nullable: false),
                    RegisterStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegisterEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Trainingtopicsrequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Branch_id = table.Column<int>(type: "int", nullable: true),
                    CertificateTemplateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainingtopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trainingtopics_Branches_Branch_id",
                        column: x => x.Branch_id,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trainingtopics_CertificateConfigs_CertificateTemplateId",
                        column: x => x.CertificateTemplateId,
                        principalTable: "CertificateConfigs",
                        principalColumn: "YearID");
                });

            migrationBuilder.CreateTable(
                name: "TrainingRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingtopicId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRegistrations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingRegistrations_Trainingtopics_TrainingtopicId",
                        column: x => x.TrainingtopicId,
                        principalTable: "Trainingtopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRegistrations_TrainingtopicId",
                table: "TrainingRegistrations",
                column: "TrainingtopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRegistrations_UserId",
                table: "TrainingRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Trainingtopics_Branch_id",
                table: "Trainingtopics",
                column: "Branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_Trainingtopics_CertificateTemplateId",
                table: "Trainingtopics",
                column: "CertificateTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingRegistrations");

            migrationBuilder.DropTable(
                name: "Trainingtopics");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
