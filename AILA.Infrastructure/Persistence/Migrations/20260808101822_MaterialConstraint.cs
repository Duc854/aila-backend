using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterialConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials",
                columns: new[] { "ModuleId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ModuleId_OrderIndex",
                table: "Materials",
                columns: new[] { "ModuleId", "OrderIndex" });
        }
    }
}
