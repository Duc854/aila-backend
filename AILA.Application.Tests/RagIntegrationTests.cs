using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AILA.Application;
using AILA.Infrastructure;
using AILA.Application.Features.Rag.Commands.IndexDocumentMaterial;
using AILA.Application.Features.Rag.Commands.CreateCourseChatSession;
using AILA.Application.Features.Rag.Commands.AskCourseRagQuestion;
using MediatR;

namespace AILA.Application.Tests
{
    public class RagIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public RagIntegrationTests(ITestOutputHelper output)
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

        [Fact]
        public async Task TestScenario1_PostgresOptimization_RAG()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var courseId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            var materialId = Guid.Parse("22222222-3333-4444-5555-666666666666");
            var accountId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

            _output.WriteLine("==================================================");
            _output.WriteLine("📚 KỊCH BẢN TEST 1: Tài liệu Tối ưu hóa Database PostgreSQL");
            _output.WriteLine("==================================================");

            // Step 1: Index Document
            var indexCmd = new IndexDocumentMaterialCommand(
                materialId,
                courseId,
                "Bài 08: Thiết kế Cơ sở Dữ liệu & Tối ưu hóa PostgreSQL với B-Tree Indexing",
                "Trong PostgreSQL, Indexing giúp tăng tốc độ truy vấn SELECT đáng kể nhưng sẽ làm giảm nhẹ tốc độ ghi INSERT/UPDATE/DELETE. Chỉ mục B-Tree (Binary Tree) là loại index mặc định thích hợp nhất cho các phép so sánh =, <, <=, >, >= và BETWEEN. Khi tạo Index cho cột thường xuyên xuất hiện trong mệnh đề WHERE hoặc JOIN, query planner sẽ ưu tiên dùng Index Scan thay vì Seq Scan (Quét toàn bộ bảng)."
            );
            var indexResult = await mediator.Send(indexCmd);
            _output.WriteLine($"[1. Index Result]: DocumentId={indexResult.KnowledgeDocumentId}, Chunks={indexResult.TotalChunks}, Status={indexResult.Status}");
            Assert.Equal("Completed", indexResult.Status);
            Assert.True(indexResult.TotalChunks > 0);

            // Step 2: Create Session
            var sessionCmd = new CreateCourseChatSessionCommand(accountId, courseId, "Hỏi đáp Tối ưu hóa Database PostgreSQL");
            var session = await mediator.Send(sessionCmd);
            _output.WriteLine($"[2. Session Created]: SessionId={session.Id}, Title='{session.Title}'");
            Assert.NotNull(session);

            // Step 3: Ask Question
            var question = "Trong PostgreSQL, tại sao B-Tree Index lại giúp tăng tốc truy vấn nhưng lại làm chậm thao tác ghi dữ liệu?";
            var askCmd = new AskCourseRagQuestionCommand(session.Id, accountId, question);
            var response = await mediator.Send(askCmd);

            _output.WriteLine($"\n[3. AI RAG Answer]:\n{response.Answer}");
            _output.WriteLine($"\n[Citations Count]: {response.Citations.Count}");
            Assert.NotEmpty(response.Answer);
        }

        [Fact]
        public async Task TestScenario2_SRSRequirements_RAG()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var courseId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            var materialId = Guid.Parse("33333333-4444-5555-6666-777777777777");
            var accountId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

            _output.WriteLine("==================================================");
            _output.WriteLine("📋 KỊCH BẢN TEST 2: Tài liệu Đặc tả Yêu cầu Kỹ thuật SRS");
            _output.WriteLine("==================================================");

            // Step 1: Index Document
            var indexCmd = new IndexDocumentMaterialCommand(
                materialId,
                courseId,
                "Bài 02: Hướng dẫn Viết Tài liệu Đặc tả Yêu cầu Kỹ thuật (SRS Standard)",
                "Tài liệu SRS (Software Requirements Specification) chuẩn bao gồm 4 phần chính: 1. Tổng quan hệ thống (Mục đích, Phạm vi dự án); 2. Yêu cầu Chức năng (Functional Requirements chia theo từng Use Case hoặc Actor); 3. Yêu cầu Phi chức năng (Non-functional Requirements như Hiệu năng, Bảo mật JWT, Quota Tokens); 4. Kiến trúc Hệ thống & Sơ đồ Cơ sở Dữ liệu (ERD). SRS là kim chỉ nam giúp BA và Coder thống nhất ngôn ngữ phát triển."
            );
            var indexResult = await mediator.Send(indexCmd);
            _output.WriteLine($"[1. Index Result]: DocumentId={indexResult.KnowledgeDocumentId}, Chunks={indexResult.TotalChunks}, Status={indexResult.Status}");
            Assert.Equal("Completed", indexResult.Status);

            // Step 2: Create Session
            var sessionCmd = new CreateCourseChatSessionCommand(accountId, courseId, "Thảo luận Quy trình Viết SRS Kỹ thuật");
            var session = await mediator.Send(sessionCmd);
            _output.WriteLine($"[2. Session Created]: SessionId={session.Id}, Title='{session.Title}'");

            // Step 3: Ask Question
            var question = "Một tài liệu đặc tả SRS kỹ thuật chuẩn cho dự án phần mềm gồm những thành phần cốt lõi nào?";
            var askCmd = new AskCourseRagQuestionCommand(session.Id, accountId, question);
            var response = await mediator.Send(askCmd);

            _output.WriteLine($"\n[3. AI RAG Answer]:\n{response.Answer}");
            Assert.NotEmpty(response.Answer);
        }

        [Fact]
        public async Task TestScenario3And4_Microservices_And_GeneralKnowledge_RAG()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var courseId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            var materialId = Guid.Parse("44444444-5555-6666-7777-888888888888");
            var accountId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

            _output.WriteLine("==================================================");
            _output.WriteLine("🔄 KỊCH BẢN TEST 3: Truy vấn Hybrid (Microservices)");
            _output.WriteLine("==================================================");

            // Step 1: Index Document
            var indexCmd = new IndexDocumentMaterialCommand(
                materialId,
                courseId,
                "Bài 10: Kiến trúc Microservices và Giao tiếp Async Message Queue",
                "Kiến trúc Microservices chia ứng dụng thành các dịch vụ độc lập giao tiếp với nhau qua REST API hoặc Message Queue (RabbitMQ / Kafka). Ưu điểm là khả năng mở rộng độc lập (Scalability) và cô lập lỗi (Fault Isolation). Nhược điểm là độ phức tạp khi quản lý giao dịch phân tán (Distributed Transactions) và nhất quán dữ liệu (Eventual Consistency)."
            );
            var indexResult = await mediator.Send(indexCmd);
            _output.WriteLine($"[1. Index Result]: DocumentId={indexResult.KnowledgeDocumentId}, Chunks={indexResult.TotalChunks}, Status={indexResult.Status}");
            Assert.Equal("Completed", indexResult.Status);

            // Step 2: Create Session
            var sessionCmd = new CreateCourseChatSessionCommand(accountId, courseId, "Phân tích Kiến trúc Microservices");
            var session = await mediator.Send(sessionCmd);
            _output.WriteLine($"[2. Session Created]: SessionId={session.Id}, Title='{session.Title}'");

            // Step 3: Ask Question 1 (Hybrid Query)
            var question1 = "Dựa vào bài học Microservices, hãy phân tích ưu nhược điểm và so sánh khi nào nên dùng RabbitMQ so với REST API?";
            var askCmd1 = new AskCourseRagQuestionCommand(session.Id, accountId, question1);
            var response1 = await mediator.Send(askCmd1);

            _output.WriteLine($"\n[3. Hybrid AI Answer]:\n{response1.Answer}");
            Assert.NotEmpty(response1.Answer);

            _output.WriteLine("\n==================================================");
            _output.WriteLine("🌐 KỊCH BẢN TEST 4: Truy vấn Tri thức Chung AI (SOLID Principles)");
            _output.WriteLine("==================================================");

            // Step 4: Ask Question 2 (General LLM Knowledge using the SAME session)
            var question2 = "Giải thích giúp mình 5 nguyên lý thiết kế hướng đối tượng SOLID trong lập trình phần mềm với ví dụ dễ hiểu.";
            var askCmd2 = new AskCourseRagQuestionCommand(session.Id, accountId, question2);
            var response2 = await mediator.Send(askCmd2);

            _output.WriteLine($"\n[4. General Knowledge AI Answer]:\n{response2.Answer}");
            Assert.NotEmpty(response2.Answer);
        }
    }
}
