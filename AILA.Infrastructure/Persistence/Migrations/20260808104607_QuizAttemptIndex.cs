using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuizAttemptIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_EnrollmentId",
                table: "QuizAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_EnrollmentId_QuizMaterialId",
                table: "QuizAttempts",
                columns: new[] { "EnrollmentId", "QuizMaterialId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_EnrollmentId_QuizMaterialId",
                table: "QuizAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_EnrollmentId",
                table: "QuizAttempts",
                column: "EnrollmentId");
        }
    }
}
