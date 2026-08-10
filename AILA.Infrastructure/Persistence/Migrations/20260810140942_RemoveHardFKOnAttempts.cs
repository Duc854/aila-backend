using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHardFKOnAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""CriteriaScore"";
                ALTER TABLE ""AITokenLogs"" DROP CONSTRAINT IF EXISTS ""FK_AITokenLogs_PracticeAttempts_AttemptId"";
                ALTER TABLE ""PromptSubmissions"" DROP CONSTRAINT IF EXISTS ""FK_PromptSubmissions_ExpertSimulationAttempts_AttemptId"";
                ALTER TABLE ""PromptSubmissions"" DROP CONSTRAINT IF EXISTS ""FK_PromptSubmissions_PracticeAttempts_AttemptId"";
                ALTER TABLE ""ContentReport"" DROP CONSTRAINT IF EXISTS ""CK_ContentReport_CourseRequired"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ContentReport_CourseRequired",
                table: "ContentReport",
                sql: "\"CourseId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AITokenLogs_PracticeAttempts_AttemptId",
                table: "AITokenLogs",
                column: "AttemptId",
                principalTable: "PracticeAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSubmissions_ExpertSimulationAttempts_AttemptId",
                table: "PromptSubmissions",
                column: "AttemptId",
                principalTable: "ExpertSimulationAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSubmissions_PracticeAttempts_AttemptId",
                table: "PromptSubmissions",
                column: "AttemptId",
                principalTable: "PracticeAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
