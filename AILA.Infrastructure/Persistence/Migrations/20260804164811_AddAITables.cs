using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAITables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CriteriaScore_PromptSubmissions_PromptSubmissionId",
                table: "CriteriaScore");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSubmissions_PracticeAttempts_PracticeAttemptId",
                table: "PromptSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PromptSubmissions_PracticeAttemptId",
                table: "PromptSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CriteriaScore_PromptSubmissionId",
                table: "CriteriaScore");

            migrationBuilder.DropColumn(
                name: "PracticeAttemptId",
                table: "PromptSubmissions");

            migrationBuilder.DropColumn(
                name: "PromptSubmissionId",
                table: "CriteriaScore");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSubmissions_AttemptId",
                table: "PromptSubmissions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScore_SubmissionId",
                table: "CriteriaScore",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CriteriaScore_PromptSubmissions_SubmissionId",
                table: "CriteriaScore",
                column: "SubmissionId",
                principalTable: "PromptSubmissions",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CriteriaScore_PromptSubmissions_SubmissionId",
                table: "CriteriaScore");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSubmissions_PracticeAttempts_AttemptId",
                table: "PromptSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PromptSubmissions_AttemptId",
                table: "PromptSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CriteriaScore_SubmissionId",
                table: "CriteriaScore");

            migrationBuilder.AddColumn<Guid>(
                name: "PracticeAttemptId",
                table: "PromptSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromptSubmissionId",
                table: "CriteriaScore",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptSubmissions_PracticeAttemptId",
                table: "PromptSubmissions",
                column: "PracticeAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScore_PromptSubmissionId",
                table: "CriteriaScore",
                column: "PromptSubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CriteriaScore_PromptSubmissions_PromptSubmissionId",
                table: "CriteriaScore",
                column: "PromptSubmissionId",
                principalTable: "PromptSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSubmissions_PracticeAttempts_PracticeAttemptId",
                table: "PromptSubmissions",
                column: "PracticeAttemptId",
                principalTable: "PracticeAttempts",
                principalColumn: "Id");
        }
    }
}
