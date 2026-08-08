using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContentReportRelationshipCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReport_Courses_CourseId",
                table: "ContentReport");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReport_Courses_CourseId",
                table: "ContentReport",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentReport_Courses_CourseId",
                table: "ContentReport");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReport_Courses_CourseId",
                table: "ContentReport",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
