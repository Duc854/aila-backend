using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAIPracticeMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskDescription",
                table: "AIPracticeMaterials",
                newName: "LearnerTask");

            migrationBuilder.AddColumn<string>(
                name: "AITask",
                table: "AIPracticeMaterials",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AITask",
                table: "AIPracticeMaterials");

            migrationBuilder.RenameColumn(
                name: "LearnerTask",
                table: "AIPracticeMaterials",
                newName: "TaskDescription");
        }
    }
}
