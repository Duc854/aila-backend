using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AILA.Application;
using AILA.Infrastructure;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using MediatR;

namespace AILA.Application.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestSubmitPromptCommandFlow()
        {
            // 1. Load appsettings.json
            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../AILA.Api"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // 2. Setup Dependency Injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);

            var serviceProvider = services.BuildServiceProvider();

            // 3. Get existing records from DB
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var enrollment = await dbContext.Enrollments.FirstOrDefaultAsync();
            Assert.NotNull(enrollment);

            var material = await dbContext.AIPracticeMaterials
                .Include(m => m.ScoringCriterias)
                .FirstOrDefaultAsync(m => m.MaterialId == enrollment.CourseId || m.ScoringCriterias.Any());

            if (material == null)
            {
                material = await dbContext.AIPracticeMaterials.Include(m => m.ScoringCriterias).FirstOrDefaultAsync();
            }
            Assert.NotNull(material);

            // Create a brand new InProgress attempt to guarantee we can submit and score it
            var attempt = new PracticeAttempt(enrollment.Id, material.MaterialId);
            await dbContext.PracticeAttempts.AddAsync(attempt);
            await dbContext.SaveChangesAsync();

            _output.WriteLine($"Created fresh PracticeAttempt ID: {attempt.Id} for testing.");

            // 4. Submit first prompt (Chat and save)
            _output.WriteLine("\n--- Submitting Prompt 1 ---");
            var command1 = new SubmitPromptCommand(attempt.Id, "sao giá cao quá vậy chị giảm xuống giúp em được không ạ");
            
            try
            {
                var result1 = await mediator.Send(command1);
                _output.WriteLine($"Prompt 1 success: Status={result1.Status}, AiResponse='{result1.AiResponse}'");
                Assert.Equal("Success", result1.Status);
                Assert.False(result1.IsViolation);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Prompt 1 Failed: {ex.Message}");
                _output.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    _output.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }

            // 5. Submit second prompt
            _output.WriteLine("\n--- Submitting Prompt 2 ---");
            var command2 = new SubmitPromptCommand(attempt.Id, "Dạ giá bên em là hợp lý nhất thị trường rồi chị.");
            try
            {
                var result2 = await mediator.Send(command2);
                _output.WriteLine($"Prompt 2 success: Status={result2.Status}, AiResponse='{result2.AiResponse}'");
                Assert.Equal("Success", result2.Status);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Prompt 2 Failed: {ex.Message}");
                throw;
            }

            // 6. Complete and score attempt
            _output.WriteLine("\n--- Completing Attempt ---");
            try
            {
                var completeCommand = new AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt.CompleteAttemptCommand(attempt.Id);
                var completeResult = await mediator.Send(completeCommand);
                _output.WriteLine($"Complete success: FinalScore={completeResult.FinalScore}, Suggestion='{completeResult.OverallSuggestion}'");
                Assert.True(completeResult.FinalScore > 0);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Complete Failed: {ex.Message}");
                _output.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}