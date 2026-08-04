using AILA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Wrappers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AILA.Api.Configurations
{
    public static class CustomAuthenticationExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };


        public static IServiceCollection AddCustomAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var secretKey =
                configuration.GetValue<string>("JwtSettings:Key");

            var issuer =
                configuration.GetValue<string>("JwtSettings:Issuer");

            var audience =
                configuration.GetValue<string>("JwtSettings:Audience");


            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException(
                    "JWT secret key (JwtSettings:Key) chưa được cấu hình.");


            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer =
                                !string.IsNullOrEmpty(issuer),
                            ValidateAudience =
                                !string.IsNullOrEmpty(audience),
                            ValidIssuer = issuer,
                            ValidAudience = audience,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(secretKey)),
                            RoleClaimType =
                                ClaimTypes.Role,
                            ClockSkew =
                                TimeSpan.Zero
                        };


                    options.Events =
                        new JwtBearerEvents
                        {
                            // 1. Token hợp lệ -> kiểm tra blacklist Redis
                            OnTokenValidated = async context =>
                            {
                                var blacklistService =
                                    context.HttpContext.RequestServices
                                        .GetRequiredService<ITokenBlacklistService>();
                                var jti =
                                    context.Principal?
                                        .FindFirst(
                                            JwtRegisteredClaimNames.Jti)
                                        ?.Value;
                                if (string.IsNullOrWhiteSpace(jti))
                                {
                                    context.Fail(
                                        "TOKEN_INVALID");

                                    return;
                                }
                                var isBlacklisted =
                                    await blacklistService
                                        .IsBlacklistedAsync(jti);
                                if (isBlacklisted)
                                {
                                    context.Fail(
                                        "TOKEN_REVOKED");
                                }
                            },


                            // 2. Authentication thất bại -> trả 401
                            OnChallenge = async context =>
                            {
                                context.HandleResponse();
                                context.Response.ContentType =
                                    "application/json";
                                context.Response.StatusCode =
                                    StatusCodes.Status401Unauthorized;
                                var code =
                                    "UNAUTHORIZED";
                                var message =
                                    "Xác thực thất bại. Vui lòng cung cấp mã token hợp lệ.";
                                if (context.AuthenticateFailure?
                                    .Message == "TOKEN_REVOKED")
                                {
                                    code =
                                        "TOKEN_REVOKED";
                                    message =
                                        "Access Token đã bị thu hồi.";
                                }
                                var response =
                                    ResponseDto<object>.FailResult(
                                        code,
                                        message);
                                await context.Response.WriteAsync(
                                    JsonSerializer.Serialize(
                                        response,
                                        JsonOptions));
                            },
                            // 3. Token hợp lệ nhưng không đủ quyền
                            OnForbidden = async context =>
                            {
                                context.Response.ContentType =
                                    "application/json";
                                context.Response.StatusCode =
                                    StatusCodes.Status403Forbidden;
                                var response =
                                    ResponseDto<object>.FailResult(
                                        "FORBIDDEN",
                                        "Bạn không có quyền truy cập vào chức năng này.");
                                await context.Response.WriteAsync(
                                    JsonSerializer.Serialize(
                                        response,
                                        JsonOptions));
                            }
                        };
                });


            return services;
        }
    }
}