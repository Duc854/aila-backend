using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

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
            });

            return services;
        }
    }
}
