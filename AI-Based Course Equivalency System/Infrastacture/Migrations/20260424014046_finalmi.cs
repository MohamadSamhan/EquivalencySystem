using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class finalmi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransferRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TransferType = table.Column<int>(type: "int", nullable: false),
                    UniversityId = table.Column<int>(type: "int", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UniversityName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MajorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OldStudentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TranscriptFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TranscriptFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_StudentId",
                table: "TransferRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_UniversityId",
                table: "TransferRequests",
                column: "UniversityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferRequests");
        }
    }
}
