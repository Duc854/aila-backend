using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseChatAndPromptSubmissionNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CourseChatSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatSessions_AccountId",
                table: "CourseChatSessions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatSessions_CourseId",
                table: "CourseChatSessions",
                column: "CourseId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseChatSessions_Courses_CourseId",
                table: "CourseChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseChatSessions_Users_AccountId",
                table: "CourseChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseChatSessions_AccountId",
                table: "CourseChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseChatSessions_CourseId",
                table: "CourseChatSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CourseChatSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
