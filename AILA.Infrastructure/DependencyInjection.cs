using Microsoft.SemanticKernel;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Infrastructure.Persistence;
using AILA.Infrastructure.Persistence.Repositories;
using AILA.Infrastructure.Persistence.Seed;
using AILA.Infrastructure.Security;
using AILA.Infrastructure.Services;
using AILA.Infrastructure.Services.AI;
using AILA.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình Database Context (PostgreSQL)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("PostgreSQL"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                )
            );

            // 2. Map dữ liệu từ appsettings.json vào Class JwtSettings và GoogleSettings của tầng Shared
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
            services.Configure<GoogleSettings>(configuration.GetSection("GoogleSettings"));
            services.Configure<AdminAccountSettings>(configuration.GetSection("AdminAccount"));
            services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));

            // 3. Đăng ký các dịch vụ hạ tầng kỹ thuật đã chốt
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<ITokenProvider, JwtTokenProvider>();
            services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IFileStorageService, CloudinaryStorageService>();

            // 4. Đăng ký mẫu thiết kế Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // 5. Đăng ký Seeder cho tài khoản hệ thống (Admin)
            services.AddScoped<AdminSeeder>();

            // 6. Đăng ký các Application services
            services.AddScoped<IQuestionExcelService, QuestionExcelService>();
            services.AddScoped<IAccountResourceRepository, AccountResourceRepository>();
            services.AddScoped<IKnowledgeChunkRepository, KnowledgeChunkRepository>();
            services.AddScoped<IPracticeAttemptRepository, PracticeAttemptRepository>();
            services.AddScoped<IAIPracticeMaterialRepository, AIPracticeMaterialRepository>();
            
            services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            services.AddScoped<IModerationService, ModerationService>();
            services.AddScoped<IPracticeChatService, PracticeChatService>();
            services.AddScoped<IPrivacyService, PrivacyService>();
            services.AddScoped<IPromptValidationService, PromptValidationService>();
            services.AddScoped<IQuotaService, QuotaService>();
            services.AddScoped<IRagChatService, RagChatService>();
            services.AddScoped<IRoleParserService, RoleParserService>();
            services.AddScoped<IScoringService, ScoringService>();

            // 7. Cấu hình Semantic Kernel Chat Completion
            services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["OpenAI:ApiKey"] ?? "dummy-key-for-build";
                var modelId = config["OpenAI:ModelId"] ?? "gpt-4o";
                var baseUrl = config["OpenAI:BaseUrl"];

                var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();

                if (!string.IsNullOrEmpty(baseUrl))
                {
                    // Đảm bảo ghi đè hoàn toàn Base URL của OpenAI sang Groq
                    var httpClient = new HttpClient(new CustomOpenAIHandler(baseUrl));
                    builder.AddOpenAIChatCompletion(modelId, apiKey, httpClient: httpClient);
                }
                else
                {
                    builder.AddOpenAIChatCompletion(modelId, apiKey);
                }

                var kernel = builder.Build();
                return kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            });

            // 8. Luồng Reset Password (UC-08): state tạm ở Redis + gửi OTP qua email nền
            services.AddPasswordReset(configuration);

            return services;
        }

        // Handler đặc biệt để bẻ lái (redirect) request của OpenAI SDK sang Groq
        private class CustomOpenAIHandler : DelegatingHandler
        {
            private readonly string _baseUrl;

            public CustomOpenAIHandler(string baseUrl)
            {
                // Loại bỏ /v1 ở cuối BaseUrl nếu có để tránh việc SDK tự append /v1/chat/completions gây ra double /v1
                _baseUrl = baseUrl.TrimEnd('/').Replace("/v1", "");
                InnerHandler = new HttpClientHandler();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri != null && request.RequestUri.Host.Contains("openai.com"))
                {
                    // request.RequestUri.PathAndQuery sẽ là "/v1/chat/completions"
                    var newUri = _baseUrl + request.RequestUri.PathAndQuery;
                    request.RequestUri = new Uri(newUri);
                }
                return base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// UC-08 Reset Password: Redis giữ toàn bộ state tạm (OTP, reset token, rate limit)
        /// nên không phải đụng tới schema DB; email OTP đi qua hàng đợi nền.
        /// </summary>
        private static IServiceCollection AddPasswordReset(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Redis chưa được cấu hình. Luồng reset password bắt buộc cần Redis.");
            }

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);

                // Redis down lúc khởi động thì API vẫn phải lên được — các endpoint khác
                // không phụ thuộc Redis. Client tự kết nối lại ở nền; trong lúc đó luồng
                // reset trả 503 chứ không cho bypass (EDGE-13).
                options.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(options);
            });

            services.AddScoped<IPasswordResetStore, RedisPasswordResetStore>();
            services.AddSingleton<IOtpService, OtpService>();

            // Hàng đợi và worker phải là singleton để sống xuyên suốt vòng đời ứng dụng.
            services.AddSingleton<IEmailQueue>(_ => new ChannelEmailQueue());
            services.AddSingleton<IEmailSender, QueuedEmailSender>();

            // Chưa cấu hình SMTP (thường là môi trường dev) thì chỉ ghi log thay vì gửi thật.
            services.AddSingleton<IEmailTransport>(sp =>
            {
                var smtpSettings = sp.GetRequiredService<IOptions<SmtpSettings>>();

                return smtpSettings.Value.IsConfigured
                    ? new SmtpEmailTransport(smtpSettings)
                    : ActivatorUtilities.CreateInstance<LoggingEmailTransport>(sp);
            });

            services.AddHostedService<EmailBackgroundService>();

            return services;
        }
    }
}
