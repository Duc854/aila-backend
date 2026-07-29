using AILA.Application.Common.Interfaces;
using AILA.Infrastructure.Persistence;
using AILA.Infrastructure.Persistence.Seed;
using AILA.Infrastructure.Security;
using AILA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
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

            return services;
        }
    }
}
