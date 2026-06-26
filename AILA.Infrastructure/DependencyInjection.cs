using AILA.Application.Common.Interfaces;
using AILA.Infrastructure.Persistence;
using AILA.Infrastructure.Services;
using AILA.Infrastructure.Security;
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

            // 2. Map dữ liệu từ appsettings.json vào Class JwtSettings của tầng Shared
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            // 3. Đăng ký các dịch vụ hạ tầng kỹ thuật đã chốt
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<ITokenProvider, JwtTokenProvider>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            // 4. Đăng ký mẫu thiết kế Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
