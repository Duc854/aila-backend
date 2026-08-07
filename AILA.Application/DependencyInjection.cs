using AILA.Application.Common.Behaviours;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.InternalService;
using FluentValidation;
using MediatR;
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

            // FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Validation Pipeline
            services.AddTransient(typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));

            // Internal Service
            services.AddScoped<IRecommendationService, RecommendationService>();
            services.AddScoped<ILearnerBehaviorService, LearnerBehaviorService>();
            // Nếu bạn có các Service nghiệp vụ thông thường (không dùng MediatR), đăng ký ở đây:
            // services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
