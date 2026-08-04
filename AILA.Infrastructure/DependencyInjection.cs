using AILA.Application.Common.Interfaces;
using AILA.Infrastructure.Persistence;
using AILA.Infrastructure.Persistence.Seed;
using AILA.Infrastructure.Security;
using AILA.Infrastructure.Services;
using AILA.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Models;
using StackExchange.Redis;

namespace AILA.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("PostgreSQL"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Options
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
            services.Configure<GoogleSettings>(configuration.GetSection("GoogleSettings"));
            services.Configure<AdminAccountSettings>(configuration.GetSection("AdminAccount"));
            services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));

            // Redis 
            services.AddRedis(configuration);

            // Security
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<ITokenProvider, JwtTokenProvider>();
            services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

            // External services
            services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IFileStorageService, CloudinaryStorageService>();

            // Core
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<AdminSeeder>();

            // Application services
            services.AddScoped<IQuestionExcelService, QuestionExcelService>();

            // Password Reset
            services.AddPasswordReset();

            return services;
        }

        private static IServiceCollection AddRedis(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Redis chưa được cấu hình.");
            }

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(connectionString);

                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 5000;
                options.KeepAlive = 60;

                return ConnectionMultiplexer.Connect(options);
            });

            services.AddScoped<
                ITokenBlacklistService,
                RedisTokenBlacklistService>();

            services.AddScoped<
                IPasswordResetStore,
                RedisPasswordResetStore>();

            return services;
        }

        private static IServiceCollection AddPasswordReset(
            this IServiceCollection services)
        {
            services.AddSingleton<IOtpService, OtpService>();

            services.AddSingleton<IEmailQueue>(
                _ => new ChannelEmailQueue());

            services.AddSingleton<IEmailSender, QueuedEmailSender>();

            services.AddSingleton<IEmailTransport>(sp =>
            {
                var smtpSettings =
                    sp.GetRequiredService<IOptions<SmtpSettings>>();

                return smtpSettings.Value.IsConfigured
                    ? new SmtpEmailTransport(smtpSettings)
                    : ActivatorUtilities.CreateInstance<LoggingEmailTransport>(sp);
            });

            services.AddHostedService<EmailBackgroundService>();

            return services;
        }
    }
}