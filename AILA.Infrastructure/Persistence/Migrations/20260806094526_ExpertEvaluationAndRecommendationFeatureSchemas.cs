using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpertEvaluationAndRecommendationFeatureSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountResourceLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiTokenLimit = table.Column<int>(type: "integer", nullable: true),
                    AiPracticeScenarioLimit = table.Column<int>(type: "integer", nullable: true),
                    ExpertEvaluationRequestLimit = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountResourceLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountResourceLimits_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountResourceUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiTokenUsed = table.Column<int>(type: "integer", nullable: false),
                    AiPracticeScenarioUsed = table.Column<int>(type: "integer", nullable: false),
                    ExpertEvaluationRequestUsed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountResourceUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountResourceUsages_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminActivityLogs_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AITokenLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    ModelId = table.Column<string>(type: "text", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITokenLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    LastIndexedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerTagScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileSeed = table.Column<int>(type: "integer", nullable: false),
                    BehaviorScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerTagScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerTagScores_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnerTagScores_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalScore = table.Column<decimal>(type: "numeric", nullable: true),
                    OverallSuggestion = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLimitPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    AiTokenLimit = table.Column<int>(type: "integer", nullable: false),
                    AiPracticeScenarioLimit = table.Column<int>(type: "integer", nullable: false),
                    ExpertEvaluationRequestLimit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLimitPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTokenQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    UsedAmountToday = table.Column<int>(type: "integer", nullable: false),
                    LastUsageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokenQuotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CitationsJson = table.Column<string>(type: "text", nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseChatMessages_CourseChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CourseChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_KnowledgeDocuments_KnowledgeDocumentId",
                        column: x => x.KnowledgeDocumentId,
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpertEvaluationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticeAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertEvaluationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpertEvaluationRequests_Experts_ExpertId",
                        column: x => x.ExpertId,
                        principalTable: "Experts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpertEvaluationRequests_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpertEvaluationRequests_PracticeAttempts_PracticeAttemptId",
                        column: x => x.PracticeAttemptId,
                        principalTable: "PracticeAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserPrompt = table.Column<string>(type: "text", nullable: false),
                    AiResponse = table.Column<string>(type: "text", nullable: false),
                    SuggestedPrompt = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    PolicyName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptSubmissions_PracticeAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "PracticeAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpertEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpertEvaluations_ExpertEvaluationRequests_EvaluationReques~",
                        column: x => x.EvaluationRequestId,
                        principalTable: "ExpertEvaluationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaScore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "IX_AccountResourceLimits_AccountId",
                table: "AccountResourceLimits",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountResourceUsages_AccountId_PeriodStart_PeriodEnd",
                table: "AccountResourceUsages",
                columns: new[] { "AccountId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminActivityLogs_AdminId_CreatedAt",
                table: "AdminActivityLogs",
                columns: new[] { "AdminId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseChatMessages_SessionId",
                table: "CourseChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScore_SubmissionId",
                table: "CriteriaScore",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertEvaluationRequests_ExpertId",
                table: "ExpertEvaluationRequests",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertEvaluationRequests_LearnerId",
                table: "ExpertEvaluationRequests",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertEvaluationRequests_PracticeAttemptId",
                table: "ExpertEvaluationRequests",
                column: "PracticeAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertEvaluations_EvaluationRequestId",
                table: "ExpertEvaluations",
                column: "EvaluationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_KnowledgeDocumentId",
                table: "KnowledgeChunks",
                column: "KnowledgeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerTagScores_LearnerId_TagId",
                table: "LearnerTagScores",
                columns: new[] { "LearnerId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerTagScores_TagId",
                table: "LearnerTagScores",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSubmissions_AttemptId",
                table: "PromptSubmissions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLimitPolicies_AccountType",
                table: "ResourceLimitPolicies",
                column: "AccountType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountResourceLimits");

            migrationBuilder.DropTable(
                name: "AccountResourceUsages");

            migrationBuilder.DropTable(
                name: "AdminActivityLogs");

            migrationBuilder.DropTable(
                name: "AITokenLogs");

            migrationBuilder.DropTable(
                name: "CourseChatMessages");

            migrationBuilder.DropTable(
                name: "CriteriaScore");

            migrationBuilder.DropTable(
                name: "ExpertEvaluations");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks");

            migrationBuilder.DropTable(
                name: "LearnerTagScores");

            migrationBuilder.DropTable(
                name: "ResourceLimitPolicies");

            migrationBuilder.DropTable(
                name: "UserTokenQuotas");

            migrationBuilder.DropTable(
                name: "CourseChatSessions");

            migrationBuilder.DropTable(
                name: "PromptSubmissions");

            migrationBuilder.DropTable(
                name: "ExpertEvaluationRequests");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropTable(
                name: "PracticeAttempts");
        }
    }
}
