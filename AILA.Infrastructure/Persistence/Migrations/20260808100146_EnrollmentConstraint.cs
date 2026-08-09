using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LearnerId",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LearnerId_CourseId",
                table: "Enrollments",
                columns: new[] { "LearnerId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LearnerId_CourseId",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LearnerId",
                table: "Enrollments",
                column: "LearnerId");
        }
    }
}
