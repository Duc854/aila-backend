using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbIter2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Learners_LearnerUserId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "course_tags_mapping");

            migrationBuilder.DropIndex(
                name: "IX_Tags_LearnerUserId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CaptionsUrl",
                table: "VideoMaterials");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "VideoMaterials");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserTokens");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "UserTokens");

            migrationBuilder.DropColumn(
                name: "LearnerUserId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "DocumentMaterials");

            migrationBuilder.DropColumn(
                name: "FileSizeKb",
                table: "DocumentMaterials");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "UserTokens",
                newName: "RefreshTokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_UserTokens_RefreshToken",
                table: "UserTokens",
                newName: "IX_UserTokens_RefreshTokenHash");

            migrationBuilder.CreateTable(
                name: "AIPracticeMaterials",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "text", nullable: false),
                    TaskDescription = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaxPromptAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIPracticeMaterials", x => x.MaterialId);
                    table.ForeignKey(
                        name: "FK_AIPracticeMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReport", x => x.Id);
                    table.CheckConstraint("CK_ContentReport_Target", "(\r\n                    (\"CourseId\" IS NOT NULL AND \"MaterialId\" IS NULL)\r\n                    OR\r\n                    (\"CourseId\" IS NULL AND \"MaterialId\" IS NOT NULL)\r\n                )");
                    table.ForeignKey(
                        name: "FK_ContentReport_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentReport_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentReport_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseTags",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTags", x => new { x.CourseId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CourseTags_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerLearningGoals",
                columns: table => new
                {
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerLearningGoals", x => new { x.LearnerId, x.TagId });
                    table.ForeignKey(
                        name: "FK_LearnerLearningGoals_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnerLearningGoals_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizMaterials",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "integer", nullable: false),
                    PassingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ShowCorrectAnswersAfterSubmission = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizMaterials", x => x.MaterialId);
                    table.ForeignKey(
                        name: "FK_QuizMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagPublishRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagPublishRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagPublishRequest_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AIPracticeMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptTemplates_AIPracticeMaterials_AIPracticeMaterialId",
                        column: x => x.AIPracticeMaterialId,
                        principalTable: "AIPracticeMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoringCriterias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AIPracticeMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringCriterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringCriterias_AIPracticeMaterials_AIPracticeMaterialId",
                        column: x => x.AIPracticeMaterialId,
                        principalTable: "AIPracticeMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepGuidances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AIPracticeMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepGuidances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepGuidances_AIPracticeMaterials_AIPracticeMaterialId",
                        column: x => x.AIPracticeMaterialId,
                        principalTable: "AIPracticeMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_QuizMaterials_QuizMaterialId",
                        column: x => x.QuizMaterialId,
                        principalTable: "QuizMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAttempts_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizAttempts_QuizMaterials_QuizMaterialId",
                        column: x => x.QuizMaterialId,
                        principalTable: "QuizMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedAnswerOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAnswers_AnswerOptions_SelectedAnswerOptionId",
                        column: x => x.SelectedAnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAnswers_QuizAttempts_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalTable: "QuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId",
                table: "AnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReport_CourseId",
                table: "ContentReport",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReport_LearnerId_CourseId_MaterialId",
                table: "ContentReport",
                columns: new[] { "LearnerId", "CourseId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentReport_MaterialId",
                table: "ContentReport",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTags_TagId",
                table: "CourseTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerLearningGoals_TagId",
                table: "LearnerLearningGoals",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_AIPracticeMaterialId",
                table: "PromptTemplates",
                column: "AIPracticeMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizMaterialId_OrderIndex",
                table: "Questions",
                columns: new[] { "QuizMaterialId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuestionId",
                table: "QuizAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuizAttemptId",
                table: "QuizAnswers",
                column: "QuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_SelectedAnswerOptionId",
                table: "QuizAnswers",
                column: "SelectedAnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_EnrollmentId",
                table: "QuizAttempts",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_QuizMaterialId",
                table: "QuizAttempts",
                column: "QuizMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringCriterias_AIPracticeMaterialId",
                table: "ScoringCriterias",
                column: "AIPracticeMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StepGuidances_AIPracticeMaterialId_OrderIndex",
                table: "StepGuidances",
                columns: new[] { "AIPracticeMaterialId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagPublishRequest_TagId",
                table: "TagPublishRequest",
                column: "TagId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentReport");

            migrationBuilder.DropTable(
                name: "CourseTags");

            migrationBuilder.DropTable(
                name: "LearnerLearningGoals");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "QuizAnswers");

            migrationBuilder.DropTable(
                name: "ScoringCriterias");

            migrationBuilder.DropTable(
                name: "StepGuidances");

            migrationBuilder.DropTable(
                name: "TagPublishRequest");

            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "QuizAttempts");

            migrationBuilder.DropTable(
                name: "AIPracticeMaterials");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "QuizMaterials");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenHash",
                table: "UserTokens",
                newName: "RefreshToken");

            migrationBuilder.RenameIndex(
                name: "IX_UserTokens_RefreshTokenHash",
                table: "UserTokens",
                newName: "IX_UserTokens_RefreshToken");

            migrationBuilder.AddColumn<string>(
                name: "CaptionsUrl",
                table: "VideoMaterials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "VideoMaterials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserTokens",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "UserTokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LearnerUserId",
                table: "Tags",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "DocumentMaterials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileSizeKb",
                table: "DocumentMaterials",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "course_tags_mapping",
                columns: table => new
                {
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_tags_mapping", x => new { x.course_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_course_tags_mapping_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_tags_mapping_Tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LearnerUserId",
                table: "Tags",
                column: "LearnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_course_tags_mapping_tag_id",
                table: "course_tags_mapping",
                column: "tag_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Learners_LearnerUserId",
                table: "Tags",
                column: "LearnerUserId",
                principalTable: "Learners",
                principalColumn: "UserId");
        }
    }
}
