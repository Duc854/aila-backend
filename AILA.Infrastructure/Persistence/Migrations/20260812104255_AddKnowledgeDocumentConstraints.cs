using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeDocumentConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_CourseId",
                table: "KnowledgeDocuments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_MaterialId",
                table: "KnowledgeDocuments",
                column: "MaterialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_CourseId",
                table: "KnowledgeChunks",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_KnowledgeDocumentId_ChunkIndex",
                table: "KnowledgeChunks",
                columns: new[] { "KnowledgeDocumentId", "ChunkIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeDocuments_Courses_CourseId",
                table: "KnowledgeDocuments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeDocuments_Materials_MaterialId",
                table: "KnowledgeDocuments",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeDocuments_Courses_CourseId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeDocuments_Materials_MaterialId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeDocuments_CourseId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeDocuments_MaterialId",
                table: "KnowledgeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_CourseId",
                table: "KnowledgeChunks");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_KnowledgeDocumentId_ChunkIndex",
                table: "KnowledgeChunks");
        }
    }
}
