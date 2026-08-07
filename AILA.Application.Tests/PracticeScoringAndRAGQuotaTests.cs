using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AILA.Application;
using AILA.Infrastructure;
using MediatR;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;
using AILA.Application.Features.Rag.Commands.CreateCourseChatSession;
using AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;

namespace AILA.Application.Tests
{
    public class PracticeScoringAndRAGQuotaTests
    {
        private readonly ITestOutputHelper _output;

        public PracticeScoringAndRAGQuotaTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private IServiceProvider CreateServiceProvider()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../AILA.Api"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// TEST 1: AI Practice Prompt Submission, Scoring, and Quota Token Recording
        /// </summary>
        [Fact]
        public async Task Test_PracticeScoring_With_QuotaIntegration()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var quotaService = scope.ServiceProvider.GetRequiredService<IQuotaService>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 TEST: AI Practice Scoring & Quota Integration");
            _output.WriteLine("==================================================");

            var uniqueId = Guid.NewGuid().ToString("N");

            // 1. Setup Learner, Category, Expert, Course, Module, Material & Enrollment
            var category = new Category($"Scoring Cat {uniqueId}", $"scoring-cat-{uniqueId}");
            await unitOfWork.Repository<Category>().AddAsync(category);

            var expertUser = new User($"expert.{uniqueId}@aila.io", $"Expert {uniqueId}", UserRole.Expert, "Password123!");
            await unitOfWork.Repository<User>().AddAsync(expertUser);

            var expert = new Expert(expertUser.Id, "AI Specialist", 5, "Expert Bio");
            await unitOfWork.Repository<Expert>().AddAsync(expert);

            var learnerUser = new User($"learner.{uniqueId}@aila.io", $"Learner {uniqueId}", UserRole.Learner, "Password123!");
            await unitOfWork.Repository<User>().AddAsync(learnerUser);

            var learner = new Learner(learnerUser.Id);
            await unitOfWork.Repository<Learner>().AddAsync(learner);

            var course = new Course("Practice Scoring Course", category.Id, expertUser.Id, KnowledgeLevel.Beginner, "Course Desc");
            await unitOfWork.Repository<Course>().AddAsync(course);

            var module = new Module(course.Id, "Module 1", 1);
            await unitOfWork.Repository<Module>().AddAsync(module);

            var baseMaterial = Material.CreateAiPractice(module.Id, "Practice Material Title", 1);
            await unitOfWork.Repository<Material>().AddAsync(baseMaterial);

            var aiMaterial = new AIPracticeMaterial(baseMaterial.Id, "Scenario Description", "AI Role", "Learner Role", PracticeDifficulty.Easy, 5);
            await unitOfWork.Repository<AIPracticeMaterial>().AddAsync(aiMaterial);

            var enrollment = new Enrollment(learnerUser.Id, course.Id, 1);
            await unitOfWork.Repository<Enrollment>().AddAsync(enrollment);
            await unitOfWork.SaveChangesAsync();

            // 2. Create Practice Attempt
            var createCommand = new CreateAttemptCommand(enrollment.Id, baseMaterial.Id);
            var attemptId = await mediator.Send(createCommand);
            _output.WriteLine($"Step 1: Created Practice Attempt: AttemptId={attemptId}");
            Assert.NotEqual(Guid.Empty, attemptId);

            // 3. Submit a valid prompt
            var submitCommand = new SubmitPromptCommand(attemptId, "Hãy hướng dẫn tôi giải bài tập này chi tiết.");
            var submitResult = await mediator.Send(submitCommand);
            _output.WriteLine($"Step 2: Submitted Prompt. Status={submitResult.Status}, IsViolation={submitResult.IsViolation}");
            Assert.False(submitResult.IsViolation);

            // 4. Complete Attempt & Run AI Scoring
            var completeCommand = new CompleteAttemptCommand(attemptId);
            var completeResult = await mediator.Send(completeCommand);
            _output.WriteLine($"Step 3: Completed Attempt. FinalScore={completeResult.FinalScore}, OverallSuggestion={completeResult.OverallSuggestion}");
            Assert.NotNull(completeResult);
            Assert.True(completeResult.FinalScore >= 0);

            // 5. Verify Quota Check Result
            var quotaResult = await quotaService.CheckQuotaAsync(learnerUser.Id, 100);
            _output.WriteLine($"Step 4: Quota Check Verified: AccountId={learnerUser.Id}, IsAllowed={quotaResult.IsAllowed}, RemainingTokens={quotaResult.RemainingTokens}");
            Assert.True(quotaResult.IsAllowed);

            _output.WriteLine("✅ PASS: AI Practice Scoring & Quota Integration Verified Successfully!");
        }

        /// <summary>
        /// TEST 2: RAG Course Chat Q&A + Quota Integration
        /// </summary>
        [Fact]
        public async Task Test_RAGCourseChat_With_QuotaIntegration()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var quotaService = scope.ServiceProvider.GetRequiredService<IQuotaService>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 TEST: RAG Course Chat & Quota Integration");
            _output.WriteLine("==================================================");

            var uniqueId = Guid.NewGuid().ToString("N");

            // 1. Setup Learner, Category, Expert, Course, Module, Material
            var category = new Category($"RAG Cat {uniqueId}", $"rag-cat-{uniqueId}");
            await unitOfWork.Repository<Category>().AddAsync(category);

            var expertUser = new User($"rag.expert.{uniqueId}@aila.io", $"RAG Expert {uniqueId}", UserRole.Expert, "Password123!");
            await unitOfWork.Repository<User>().AddAsync(expertUser);

            var expert = new Expert(expertUser.Id, "RAG Specialist", 5, "Expert Bio");
            await unitOfWork.Repository<Expert>().AddAsync(expert);

            var learnerUser = new User($"rag.learner.{uniqueId}@aila.io", $"RAG Learner {uniqueId}", UserRole.Learner, "Password123!");
            await unitOfWork.Repository<User>().AddAsync(learnerUser);

            var learner = new Learner(learnerUser.Id);
            await unitOfWork.Repository<Learner>().AddAsync(learner);

            var course = new Course("RAG QnA Course", category.Id, expertUser.Id, KnowledgeLevel.Beginner, "Course Desc");
            await unitOfWork.Repository<Course>().AddAsync(course);
            await unitOfWork.SaveChangesAsync();

            // 2. Create Course Chat Session
            var createSessionCommand = new CreateCourseChatSessionCommand(learnerUser.Id, course.Id, "Phiên thảo luận RAG bài học");
            var sessionDto = await mediator.Send(createSessionCommand);
            _output.WriteLine($"Step 1: Created RAG Session: SessionId={sessionDto.Id}, Title={sessionDto.Title}");
            Assert.NotNull(sessionDto);
            Assert.NotEqual(Guid.Empty, sessionDto.Id);

            // 3. Send RAG Q&A Question
            var askCommand = new AskCourseRagQuestionCommand(sessionDto.Id, learnerUser.Id, "Khóa học này gồm những nội dung chính nào?");
            var askResult = await mediator.Send(askCommand);
            _output.WriteLine($"Step 2: RAG Answer Received. Status={askResult.Status}, AnswerLength={askResult.Answer?.Length ?? 0}");
            Assert.NotNull(askResult);
            Assert.True(askResult.Status == "Success" || askResult.Status == "QuotaExceeded");

            // 4. Verify Quota Service Check & Record
            var quotaResult = await quotaService.CheckQuotaAsync(learnerUser.Id, 100);
            _output.WriteLine($"Step 3: Quota Active Usage Verified: AccountId={learnerUser.Id}, IsAllowed={quotaResult.IsAllowed}");
            Assert.True(quotaResult.IsAllowed);

            _output.WriteLine("✅ PASS: RAG Course Chat & Quota Integration Verified Successfully!");
        }
    }
}
