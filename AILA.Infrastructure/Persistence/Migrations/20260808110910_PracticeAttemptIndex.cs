using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PracticeAttemptIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PracticeAttempts_EnrollmentId",
                table: "PracticeAttempts",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAttempts_EnrollmentId_MaterialId",
                table: "PracticeAttempts",
                columns: new[] { "EnrollmentId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAttempts_MaterialId",
                table: "PracticeAttempts",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PracticeAttempts_EnrollmentId",
                table: "PracticeAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeAttempts_EnrollmentId_MaterialId",
                table: "PracticeAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeAttempts_MaterialId",
                table: "PracticeAttempts");
        }
    }
}
