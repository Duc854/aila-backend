using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using AILA.Infrastructure.Persistence;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace AILA.Application.Tests
{
    public class UnitTest2
    {
        private readonly ITestOutputHelper _output;

        public UnitTest2(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task DumpPromptSubmissions()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../AILA.Api"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connStr = configuration.GetConnectionString("PostgreSQL");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connStr);
            using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            var submissions = await dbContext.PromptSubmissions
                .ToListAsync();

            _output.WriteLine($"Total submissions in DB: {submissions.Count}");
            foreach (var s in submissions)
            {
                _output.WriteLine($"Submission ID: {s.Id}, AttemptId: {s.AttemptId}, UserPrompt: '{s.UserPrompt}', IsRejected: {s.IsRejected}");
            }
        }
    }
}
