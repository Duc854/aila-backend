using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AILA.Application;
using AILA.Infrastructure;
using AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;
using AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;
using MediatR;
using AILA.Domain.Entities;
using AILA.Application.Common.Interfaces;

namespace AILA.Application.Tests
{
    public class AdminAIReportsIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public AdminAIReportsIntegrationTests(ITestOutputHelper output)
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
        /// TEST UC-87: Admin Review AI Resource Consumption Reports & Estimated Costs
        /// </summary>
        [Fact]
        public async Task Test_UC87_GetAIResourceConsumptionReport()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 UC-87 TEST: Get AI Resource Consumption Report");
            _output.WriteLine("==================================================");

            var query = new GetAIResourceConsumptionReportQuery(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(1));
            var result = await mediator.Send(query);

            _output.WriteLine($"[Report Result]: TotalTokens={result.TotalTokens:N0}, TotalRequests={result.TotalRequests}, TotalEstimatedCostUsd=${result.TotalEstimatedCostUsd:F6}");
            Assert.NotNull(result);
            Assert.NotNull(result.ModelBreakdown);
        }

        /// <summary>
        /// TEST UC-88: Admin Review AI Policy Violation Audit Records
        /// </summary>
        [Fact]
        public async Task Test_UC88_GetAIPolicyViolations()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 UC-88 TEST: Get AI Policy Violation Audit Records");
            _output.WriteLine("==================================================");

            // 1. Ghi nhận 1 vi phạm mẫu
            var testUserId = Guid.NewGuid();
            var sampleAttemptId = Guid.NewGuid();
            var sampleViolation = new UserViolationRecord(
                testUserId,
                "PromptValidationViolation",
                "PIIProtection",
                "Phát hiện email cá nhân trong prompt",
                attemptId: sampleAttemptId,
                severity: "High");

            await unitOfWork.Repository<UserViolationRecord>().AddAsync(sampleViolation);
            await unitOfWork.SaveChangesAsync();

            // 2. Admin gọi Query lấy danh sách vi phạm
            var query = new GetAIPolicyViolationsQuery(ViolationType: "PromptValidationViolation", Severity: "High", PageNumber: 1, PageSize: 10);
            var result = await mediator.Send(query);

            _output.WriteLine($"[Violations Result]: TotalCount={result.TotalCount}, ItemsInPage={result.Items.Count}");
            Assert.NotNull(result);
            Assert.True(result.TotalCount > 0);
            Assert.Contains(result.Items, v => v.UserId == testUserId);
        }
    }
}
