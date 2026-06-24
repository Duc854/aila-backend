using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Wrappers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AILA.Api.Configurations
{
    public static class CustomAuthenticationExtensions
    {
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration.GetValue<string>("JwtSettings:Key");
            var issuer = configuration.GetValue<string>("JwtSettings:Issuer");
            var audience = configuration.GetValue<string>("JwtSettings:Audience");

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT secret key (JwtSettings:Key) chưa được cấu hình.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // 1. Xử lý khi Token không hợp lệ, hết hạn hoặc không được gửi lên (401 Unauthorized)
                    OnChallenge = async context =>
                    {
                        // Chặn cơ chế sinh phản hồi mặc định của thư viện để tránh xung đột header
                        context.HandleResponse();

                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        var responseDto = ResponseDto<object>.FailResult(
                            "UNAUTHORIZED",
                            "Xác thực thất bại. Vui lòng cung cấp mã token hợp lệ."
                        );

                        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var jsonResult = JsonSerializer.Serialize(responseDto, jsonOptions);

                        await context.Response.WriteAsync(jsonResult);
                    },

                    // 2. Xử lý khi Token hợp lệ nhưng sai Quyền/Role truy cập (403 Forbidden)
                    OnForbidden = async context =>
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;

                        var responseDto = ResponseDto<object>.FailResult(
                            "FORBIDDEN",
                            "Bạn không có quyền truy cập vào chức năng này."
                        );

                        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var jsonResult = JsonSerializer.Serialize(responseDto, jsonOptions);

                        await context.Response.WriteAsync(jsonResult);
                    }
                };
            });

            return services;
        }
    }
}
