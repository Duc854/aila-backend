using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Tự động quét và đăng ký tất cả Handlers của MediatR trong tầng Application
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Nếu bạn có các Service nghiệp vụ thông thường (không dùng MediatR), đăng ký ở đây:
            // services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
