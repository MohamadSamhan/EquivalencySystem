using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class addfile2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedFileHash",
                table: "StudentCourses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedFileName",
                table: "StudentCourses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedFilePath",
                table: "StudentCourses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedFileHash",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "UploadedFileName",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "UploadedFilePath",
                table: "StudentCourses");
        }
    }
}
