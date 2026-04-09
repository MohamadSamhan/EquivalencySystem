using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class addfile3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceFilePath",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceFilePath",
                table: "Courses");
        }
    }
}
