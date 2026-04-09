using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class Universitie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniversityName",
                table: "StudentCourses");

            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                table: "StudentCourses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Universities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_UniversityId",
                table: "StudentCourses",
                column: "UniversityId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourses_Universities_UniversityId",
                table: "StudentCourses",
                column: "UniversityId",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourses_Universities_UniversityId",
                table: "StudentCourses");

            migrationBuilder.DropTable(
                name: "Universities");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_UniversityId",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                table: "StudentCourses");

            migrationBuilder.AddColumn<string>(
                name: "UniversityName",
                table: "StudentCourses",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }
    }
}
