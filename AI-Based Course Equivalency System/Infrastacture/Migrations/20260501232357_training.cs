using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class training : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingCertificateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TrainingTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TrainingProvider = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TrainingHours = table.Column<int>(type: "int", nullable: false),
                    CertificateFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CertificateFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByDoctorId = table.Column<int>(type: "int", nullable: true),
                    ReviewerNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCertificateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingCertificateRequests_Users_ReviewedByDoctorId",
                        column: x => x.ReviewedByDoctorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingCertificateRequests_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCertificateRequests_ReviewedByDoctorId",
                table: "TrainingCertificateRequests",
                column: "ReviewedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCertificateRequests_StudentId",
                table: "TrainingCertificateRequests",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingCertificateRequests");
        }
    }
}
