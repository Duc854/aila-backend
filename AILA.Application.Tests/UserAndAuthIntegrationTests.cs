using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AILA.Application;
using AILA.Infrastructure;
using AILA.Infrastructure.Persistence;
using AILA.Application.Features.Authentication.Commands.Register;
using AILA.Application.Features.Authentication.Commands.LearnerLogin;
using AILA.Application.Features.Authentication.Commands.Logout;
using AILA.Application.Features.Users.Commands.UpdateUserStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AILA.Domain.Entities;

namespace AILA.Application.Tests
{
    public class UserAndAuthIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public UserAndAuthIntegrationTests(ITestOutputHelper output)
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

        /// <summary>
        /// TEST 1: Cập nhật trạng thái người dùng (Update Status User - Ban / Active)
        /// </summary>
        [Fact]
        public async Task Test_UpdateUserStatus()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 TEST 1: Cập nhật trạng thái User (Update User Status)");
            _output.WriteLine("==================================================");

            // 1. Tạo tài khoản mẫu để test
            var testEmail = $"status_test_{Guid.NewGuid():N}@example.com";
            var registerRes = await mediator.Send(new RegisterCommand
            {
                Email = testEmail,
                Password = "Password123!",
                FullName = "User Status Test"
            });
            Assert.True(registerRes.Success);
            var userId = registerRes.Data!.UserId;
            _output.WriteLine($"[Created User]: Id={userId}, Email='{testEmail}', IsActive=true");

            // 2. Khóa tài khoản (Deactivate / Ban)
            var banCmd = new UpdateUserStatusCommand(userId, IsActive: false);
            var banRes = await mediator.Send(banCmd);
            _output.WriteLine($"[Ban Result]: Success={banRes.Success}, IsActive={banRes.Data?.IsActive}");
            Assert.True(banRes.Success);
            Assert.False(banRes.Data!.IsActive);

            // 3. Mở khóa tài khoản (Activate / Unban)
            var unbanCmd = new UpdateUserStatusCommand(userId, IsActive: true);
            var unbanRes = await mediator.Send(unbanCmd);
            _output.WriteLine($"[Unban Result]: Success={unbanRes.Success}, IsActive={unbanRes.Data?.IsActive}");
            Assert.True(unbanRes.Success);
            Assert.True(unbanRes.Data!.IsActive);
        }

        /// <summary>
        /// TEST 2: Đăng nhập bằng tài khoản bị khóa (Login with Banned User)
        /// </summary>
        [Fact]
        public async Task Test_LoginWithBannedAccount()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 TEST 2: Đăng nhập bằng tài khoản bị khóa (Banned Account)");
            _output.WriteLine("==================================================");

            // 1. Đăng ký tài khoản
            var testEmail = $"banned_user_{Guid.NewGuid():N}@example.com";
            var password = "Password123!";
            var registerRes = await mediator.Send(new RegisterCommand
            {
                Email = testEmail,
                Password = password,
                FullName = "Banned User"
            });
            Assert.True(registerRes.Success);
            var userId = registerRes.Data!.UserId;

            // 2. Tiến hành Khóa tài khoản
            await mediator.Send(new UpdateUserStatusCommand(userId, IsActive: false));
            _output.WriteLine($"[Account Banned]: Email='{testEmail}'");

            // 3. Thử Đăng nhập bằng tài khoản bị khóa
            var loginRes = await mediator.Send(new LearnerLoginCommand
            {
                Email = testEmail,
                Password = password
            });

            _output.WriteLine($"[Login Result]: Success={loginRes.Success}, ErrorCode='{loginRes.ErrorCode}', ErrorMessage='{loginRes.ErrorMessage}'");
            Assert.False(loginRes.Success);
            Assert.Equal("ACCOUNT_BANNED", loginRes.ErrorCode);
            Assert.Contains("khóa", loginRes.ErrorMessage);
        }

        /// <summary>
        /// TEST 3: Đăng ký với Email rác / không đúng định dạng (Register with Trash Email)
        /// </summary>
        [Theory]
        [InlineData("email_rac")]
        [InlineData("email_rac@")]
        [InlineData("@domain.com")]
        [InlineData("not_an_email.com")]
        public async Task Test_RegisterWithTrashEmail(string trashEmail)
        {
            _output.WriteLine("==================================================");
            _output.WriteLine($"🧪 TEST 3: Đăng ký Email rác ('{trashEmail}')");
            _output.WriteLine("==================================================");

            var validator = new RegisterCommandValidator();
            var command = new RegisterCommand
            {
                Email = trashEmail,
                Password = "Password123!",
                FullName = "Spam User"
            };

            var validationResult = await validator.ValidateAsync(command);

            _output.WriteLine($"[Validation Result]: IsValid={validationResult.IsValid}");
            foreach (var err in validationResult.Errors)
            {
                _output.WriteLine($"  - Error: {err.PropertyName} -> {err.ErrorMessage}");
            }

            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors, e => e.PropertyName == "Email" && e.ErrorMessage.Contains("định dạng"));
        }

        /// <summary>
        /// TEST 4: Đăng xuất người dùng (Logout Flow & Revoke Token)
        /// </summary>
        [Fact]
        public async Task Test_LogoutFlow()
        {
            var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _output.WriteLine("==================================================");
            _output.WriteLine("🧪 TEST 4: Đăng xuất (Logout & Revoke Token)");
            _output.WriteLine("==================================================");

            // 1. Đăng ký & Đăng nhập lấy RefreshToken
            var testEmail = $"logout_test_{Guid.NewGuid():N}@example.com";
            var password = "Password123!";
            await mediator.Send(new RegisterCommand { Email = testEmail, Password = password, FullName = "Logout User" });

            var loginRes = await mediator.Send(new LearnerLoginCommand { Email = testEmail, Password = password });
            Assert.True(loginRes.Success);
            var userId = loginRes.Data!.UserId;
            var refreshToken = loginRes.Data.RefreshToken;

            _output.WriteLine($"[Login Success]: UserId={userId}, RefreshToken='{refreshToken[..10]}...'");

            // 2. Thực hiện Logout
            var logoutRes = await mediator.Send(new LogoutCommand(refreshToken, "test-jti", DateTime.UtcNow.AddHours(1)));

            _output.WriteLine($"[Logout Result]: Success={logoutRes}");
            Assert.True(logoutRes);

            // 3. Kiểm tra Token trong DB đã được thu hồi (IsRevoked = true)
            var tokenInDb = await dbContext.UserTokens.FirstOrDefaultAsync(t => t.UserId == userId);
            Assert.NotNull(tokenInDb);
            _output.WriteLine($"[Token Status in DB]: IsRevoked={tokenInDb.IsRevoked}");
            Assert.True(tokenInDb.IsRevoked);
        }
    }
}
