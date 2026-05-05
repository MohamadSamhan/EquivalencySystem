using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastacture.Migrations
{
    /// <inheritdoc />
    public partial class addadminandmore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "StudentCourses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingHours",
                table: "StudentCourses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "EquivalencyRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "EquivalencyRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByDoctorId",
                table: "EquivalencyRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerNotes",
                table: "EquivalencyRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquivalencyRequests_ReviewedByDoctorId",
                table: "EquivalencyRequests",
                column: "ReviewedByDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquivalencyRequests_Users_ReviewedByDoctorId",
                table: "EquivalencyRequests",
                column: "ReviewedByDoctorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquivalencyRequests_Users_ReviewedByDoctorId",
                table: "EquivalencyRequests");

            migrationBuilder.DropIndex(
                name: "IX_EquivalencyRequests_ReviewedByDoctorId",
                table: "EquivalencyRequests");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "TrainingHours",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "EquivalencyRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "EquivalencyRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByDoctorId",
                table: "EquivalencyRequests");

            migrationBuilder.DropColumn(
                name: "ReviewerNotes",
                table: "EquivalencyRequests");
        }
    }
}
