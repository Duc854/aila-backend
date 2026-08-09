using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriteriaScore");

            migrationBuilder.DropColumn(
                name: "AttemptId",
                table: "UserViolationRecords");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "UserViolationRecords");

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

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AdminActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "ViolatingPrompt",
                table: "UserViolationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CourseChatSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "AITokenLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ModelId",
                table: "AITokenLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_UserViolationRecords_UserId",
                table: "UserViolationRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertSimulationAttempts_MaterialId",
                table: "ExpertSimulationAttempts",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatSessions_AccountId",
                table: "CourseChatSessions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatSessions_CourseId",
                table: "CourseChatSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AITokenLogs_AccountId",
                table: "AITokenLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AITokenLogs_AttemptId",
                table: "AITokenLogs",
                column: "AttemptId");

            migrationBuilder.AddForeignKey(
                name: "FK_AITokenLogs_PracticeAttempts_AttemptId",
                table: "AITokenLogs",
                column: "AttemptId",
                principalTable: "PracticeAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AITokenLogs_Users_AccountId",
                table: "AITokenLogs",
                column: "AccountId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseChatSessions_Courses_CourseId",
                table: "CourseChatSessions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseChatSessions_Users_AccountId",
                table: "CourseChatSessions",
                column: "AccountId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpertSimulationAttempts_AIPracticeMaterials_MaterialId",
                table: "ExpertSimulationAttempts",
                column: "MaterialId",
                principalTable: "AIPracticeMaterials",
                principalColumn: "MaterialId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeAttempts_AIPracticeMaterials_MaterialId",
                table: "PracticeAttempts",
                column: "MaterialId",
                principalTable: "AIPracticeMaterials",
                principalColumn: "MaterialId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeAttempts_Enrollments_EnrollmentId",
                table: "PracticeAttempts",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserViolationRecords_Users_UserId",
                table: "UserViolationRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AITokenLogs_PracticeAttempts_AttemptId",
                table: "AITokenLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AITokenLogs_Users_AccountId",
                table: "AITokenLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseChatSessions_Courses_CourseId",
                table: "CourseChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseChatSessions_Users_AccountId",
                table: "CourseChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpertSimulationAttempts_AIPracticeMaterials_MaterialId",
                table: "ExpertSimulationAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_PracticeAttempts_AIPracticeMaterials_MaterialId",
                table: "PracticeAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_PracticeAttempts_Enrollments_EnrollmentId",
                table: "PracticeAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserViolationRecords_Users_UserId",
                table: "UserViolationRecords");

            migrationBuilder.DropIndex(
                name: "IX_UserViolationRecords_UserId",
                table: "UserViolationRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpertSimulationAttempts_MaterialId",
                table: "ExpertSimulationAttempts");

            migrationBuilder.DropIndex(
                name: "IX_CourseChatSessions_AccountId",
                table: "CourseChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseChatSessions_CourseId",
                table: "CourseChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_AITokenLogs_AccountId",
                table: "AITokenLogs");

            migrationBuilder.DropIndex(
                name: "IX_AITokenLogs_AttemptId",
                table: "AITokenLogs");

            migrationBuilder.DropColumn(
                name: "ViolatingPrompt",
                table: "UserViolationRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "AttemptId",
                table: "UserViolationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "UserViolationRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");

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

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CourseChatSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "AITokenLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ModelId",
                table: "AITokenLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "AdminActivityLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AdminActivityLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AdminActivityLogs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

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
