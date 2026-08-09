using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAINavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_PracticeAttempts_EnrollmentId",
                table: "PracticeAttempts",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAttempts_MaterialId",
                table: "PracticeAttempts",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertSimulationAttempts_MaterialId",
                table: "ExpertSimulationAttempts",
                column: "MaterialId");

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
                name: "IX_PracticeAttempts_EnrollmentId",
                table: "PracticeAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeAttempts_MaterialId",
                table: "PracticeAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ExpertSimulationAttempts_MaterialId",
                table: "ExpertSimulationAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AITokenLogs_AccountId",
                table: "AITokenLogs");

            migrationBuilder.DropIndex(
                name: "IX_AITokenLogs_AttemptId",
                table: "AITokenLogs");

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
        }
    }
}
