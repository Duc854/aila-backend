using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContentReportStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentReport_LearnerId_CourseId_MaterialId",
                table: "ContentReport");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReport_LearnerId_CourseId_MaterialId",
                table: "ContentReport",
                columns: new[] { "LearnerId", "CourseId", "MaterialId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentReport_LearnerId_CourseId_MaterialId",
                table: "ContentReport");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReport_LearnerId_CourseId_MaterialId",
                table: "ContentReport",
                columns: new[] { "LearnerId", "CourseId", "MaterialId" },
                unique: true);
        }
    }
}
