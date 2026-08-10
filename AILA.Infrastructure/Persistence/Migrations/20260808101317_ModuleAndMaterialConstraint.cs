using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModuleAndMaterialConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Modules_CourseId",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ModuleId",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CourseId_OrderIndex",
                table: "Modules",
                columns: new[] { "CourseId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials",
                columns: new[] { "ModuleId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Modules_CourseId_OrderIndex",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CourseId",
                table: "Modules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ModuleId",
                table: "Materials",
                column: "ModuleId");
        }
    }
}
