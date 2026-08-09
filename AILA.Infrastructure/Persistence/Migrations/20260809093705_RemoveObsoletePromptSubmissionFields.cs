using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoletePromptSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriteriaScore");

            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "PromptSubmissions");

            migrationBuilder.DropColumn(
                name: "PolicyName",
                table: "PromptSubmissions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PromptSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestedPrompt",
                table: "PromptSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "PromptSubmissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PolicyName",
                table: "PromptSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PromptSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedPrompt",
                table: "PromptSubmissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CriteriaScore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaScore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriteriaScore_PromptSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "PromptSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScore_SubmissionId",
                table: "CriteriaScore",
                column: "SubmissionId");
        }
    }
}
