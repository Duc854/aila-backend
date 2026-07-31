using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Common.Models;
using AILA.Application.Features.Authentication.Commands;
using AILA.Application.Features.Authentication.Commands.ConfirmPasswordReset;
using AILA.Application.Features.Authentication.Commands.RequestPasswordReset;
using AILA.Application.Features.Authentication.Commands.VerifyPasswordResetOtp;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;

namespace AILA.Application.Tests.Features.Authentication
{
    /// <summary>
    /// Sheet: AuthService · UC-08 Reset Password · TC-UNIT-AuthService-007 → 013.
    ///
    /// Workbook mô tả luồng này bằng 3 method kiểu Java (<c>requestResetOtp</c>,
    /// <c>verifyResetOtp</c>, <c>resetPassword</c>). Trong code .NET chúng là 3 handler:
    /// RequestPasswordReset → VerifyPasswordResetOtp → ConfirmPasswordReset.
    ///
    /// Notes cũ trong workbook ghi "⚠ không tồn tại tính năng reset password/OTP" — đã LỖI THỜI,
    /// tính năng nay đã có đầy đủ.
    /// </summary>
    public class PasswordResetCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IPasswordResetStore> _store = new();
        private readonly Mock<IOtpService> _otp = new();
        private readonly Mock<IEmailSender> _email = new();
        private readonly Mock<IPasswordHasher> _hasher = new();

        // MinRequestDurationMs = 0: bỏ phần đệm chống timing-attack, nếu không mỗi test
        // RequestPasswordReset sẽ tốn 350ms thật.
        private readonly PasswordResetSettings _settings = new()
        {
            OtpTtlSeconds = 300,
            ResetTokenTtlSeconds = 600,
            MaxVerifyAttempts = 5,
            MaxOtpRequestsPerEmail = 5,
            MaxOtpRequestsPerIp = 20,
            MinRequestDurationMs = 0,
            RejectPasswordSameAsCurrent = true
        };

        public PasswordResetCommandHandlerTests()
        {
            _uow.Setup(u => u.Users).Returns(_users.Object);
        }

        private RequestPasswordResetCommandHandler RequestHandler() => new(
            _uow.Object, _store.Object, _otp.Object, _email.Object,
            Options.Create(_settings), NullLogger<RequestPasswordResetCommandHandler>.Instance);

        private VerifyPasswordResetOtpCommandHandler VerifyHandler() => new(
            _uow.Object, _store.Object, _otp.Object,
            Options.Create(_settings), NullLogger<VerifyPasswordResetOtpCommandHandler>.Instance);

        private ConfirmPasswordResetCommandHandler ConfirmHandler() => new(
            _uow.Object, _store.Object, _hasher.Object,
            Options.Create(_settings), NullLogger<ConfirmPasswordResetCommandHandler>.Instance);

        // ============================================================ TC-007
        // Covers: Main Flow step 2 — email hợp lệ thì sinh OTP, lưu HASH (không lưu OTP thô)
        // và đẩy mail đi.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-007")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task RequestReset_ValidEmail_OtpSavedAndSent()
        {
            var user = new UserBuilder().WithEmail("learner@aila.com").WithFullName("Nguyen An").Build();
            _users.Setup(r => r.GetByEmailAsync("learner@aila.com")).ReturnsAsync(user);
            _otp.Setup(o => o.GenerateOtp()).Returns("123456");
            _otp.Setup(o => o.HashOtp("learner@aila.com", "123456")).Returns("OTP_HASH");

            var result = await RequestHandler().Handle(
                new RequestPasswordResetCommand("learner@aila.com", "1.2.3.4"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(_settings.OtpTtlSeconds, result.Data!.OtpExpiresInSeconds);
            _store.Verify(s => s.SaveOtpAsync("learner@aila.com", "OTP_HASH", It.IsAny<CancellationToken>()), Times.Once);
            _email.Verify(e => e.SendPasswordResetOtpAsync(
                user.Email, user.FullName, "123456", 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-008
        // Covers: AF-01 invalid email — KHÔNG sinh OTP, nhưng response phải giống hệt nhánh
        // thành công. Đây là yêu cầu chống account enumeration, nên khẳng định "vẫn Success"
        // là có chủ đích chứ không phải nhầm.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-008")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task RequestReset_UnknownEmail_NeutralNoOtp()
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await RequestHandler().Handle(
                new RequestPasswordResetCommand("nouser@aila.com", null), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(_settings.OtpTtlSeconds, result.Data!.OtpExpiresInSeconds);
            _otp.Verify(o => o.GenerateOtp(), Times.Never);
            _store.Verify(s => s.SaveOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _email.Verify(e => e.SendPasswordResetOtpAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-008b
        // Cùng nguyên tắc trung tính: tài khoản bị khoá cũng không được sinh OTP.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-008")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task RequestReset_InactiveUser_NeutralNoOtp()
        {
            _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                  .ReturnsAsync(new UserBuilder().Inactive().Build());

            var result = await RequestHandler().Handle(
                new RequestPasswordResetCommand("learner@aila.com", null), CancellationToken.None);

            Assert.True(result.Success);
            _otp.Verify(o => o.GenerateOtp(), Times.Never);
        }

        // ============================================================ TC-009
        // Covers: AF-02 wrong OTP — trả mã lỗi chung và ĐẾM số lần sai để chặn brute-force.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-009")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task VerifyOtp_WrongOtp_GenericErrorCounted()
        {
            _store.Setup(s => s.GetOtpAsync("learner@aila.com", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new OtpEntry("OTP_HASH", 0));
            _otp.Setup(o => o.VerifyOtp("learner@aila.com", "999999", "OTP_HASH")).Returns(false);
            _store.Setup(s => s.IncrementOtpAttemptAsync("learner@aila.com", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(1);

            var result = await VerifyHandler().Handle(
                new VerifyPasswordResetOtpCommand("learner@aila.com", "999999"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
            _store.Verify(s => s.IncrementOtpAttemptAsync("learner@aila.com", It.IsAny<CancellationToken>()), Times.Once);
            _store.Verify(s => s.SaveResetTokenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-009b
        // BVA trên ngưỡng MaxVerifyAttempts: đủ ngưỡng thì OTP bị huỷ hẳn, dưới ngưỡng thì giữ.
        [Theory]
        [InlineData(4, false)]  // ngưỡng - 1 → chưa huỷ
        [InlineData(5, true)]   // đúng ngưỡng → huỷ
        [InlineData(6, true)]   // vượt ngưỡng → huỷ
        [Trait("TC", "TC-UNIT-AuthService-009")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task VerifyOtp_AttemptThreshold_DeletesOtp(
            int attempts, bool shouldDelete)
        {
            _store.Setup(s => s.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new OtpEntry("OTP_HASH", 0));
            _otp.Setup(o => o.VerifyOtp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _store.Setup(s => s.IncrementOtpAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(attempts);

            await VerifyHandler().Handle(
                new VerifyPasswordResetOtpCommand("learner@aila.com", "999999"), CancellationToken.None);

            _store.Verify(s => s.DeleteOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                shouldDelete ? Times.Once() : Times.Never());
        }

        // ============================================================ TC-010
        // Covers: BR-01 OTP hết hạn sau 5 phút.
        // Lưu ý phạm vi: TTL do store (Redis) tự hết hạn, handler chỉ thấy "không còn bản ghi".
        // Biên 5 phút ±1s KHÔNG kiểm được ở L1 — thuộc integration test của IPasswordResetStore.
        // Ở L1 chỉ khẳng định: store trả null ⇒ coi như hết hạn, cùng mã lỗi chung với OTP sai.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-010")]
        [Trait("UC", "UC-08")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "High")]
        public async Task VerifyOtp_NoActiveOtp_SameGenericError()
        {
            _store.Setup(s => s.GetOtpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((OtpEntry?)null);

            var result = await VerifyHandler().Handle(
                new VerifyPasswordResetOtpCommand("learner@aila.com", "123456"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(PasswordResetErrorCodes.InvalidOrExpiredOtp, result.ErrorCode);
            _otp.Verify(o => o.VerifyOtp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ============================================================ TC-011
        // Covers: BR-02 single-use — OTP đúng thì bị xoá NGAY trước khi cấp reset token,
        // nên lần dùng thứ hai rơi vào nhánh "không còn bản ghi" (TC-010).
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-011")]
        [Trait("UC", "UC-08")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task VerifyOtp_CorrectOtp_DeletesThenIssues()
        {
            var user = new UserBuilder().WithEmail("learner@aila.com").Build();
            _store.Setup(s => s.GetOtpAsync("learner@aila.com", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new OtpEntry("OTP_HASH", 0));
            _otp.Setup(o => o.VerifyOtp("learner@aila.com", "123456", "OTP_HASH")).Returns(true);
            _otp.Setup(o => o.GenerateResetToken()).Returns("RESET_TOKEN");
            _users.Setup(r => r.GetByEmailAsync("learner@aila.com")).ReturnsAsync(user);

            var result = await VerifyHandler().Handle(
                new VerifyPasswordResetOtpCommand("learner@aila.com", "123456"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("RESET_TOKEN", result.Data!.ResetToken);
            Assert.Equal(_settings.ResetTokenTtlSeconds, result.Data.ExpiresInSeconds);
            _store.Verify(s => s.DeleteOtpAsync("learner@aila.com", It.IsAny<CancellationToken>()), Times.Once);
            _store.Verify(s => s.SaveResetTokenAsync("RESET_TOKEN", user.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-012
        // Covers: Main Flow step 6 — token hợp lệ + password đạt policy ⇒ đổi password,
        // tiêu thụ token đúng một lần, lưu đúng một lần.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-012")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ConfirmReset_ValidToken_UpdatesAndBurns()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _store.Setup(s => s.PeekResetTokenAsync("RESET_TOKEN", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user.Id);
            _store.Setup(s => s.ConsumeResetTokenAsync("RESET_TOKEN", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user.Id);
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("NewPass1", "$old")).Returns(false);
            _hasher.Setup(h => h.HashPassword("NewPass1")).Returns("$new");

            var result = await ConfirmHandler().Handle(
                new ConfirmPasswordResetCommand("RESET_TOKEN", "NewPass1", "NewPass1"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("$new", user.PasswordHash);
            _store.Verify(s => s.ConsumeResetTokenAsync("RESET_TOKEN", It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-013
        // Covers: AF-03 invalid password. Điểm mấu chốt: token KHÔNG được tiêu thụ khi password
        // sai — nếu không, một lần gõ nhầm sẽ đốt token và bắt người dùng xin lại OTP.
        [Theory]
        [InlineData("NewPass1", "Different1")]  // confirm không khớp
        [InlineData("short", "short")]          // dưới 8 ký tự
        [InlineData("", "")]                    // rỗng
        [Trait("TC", "TC-UNIT-AuthService-013")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task ConfirmReset_BadPassword_KeepsToken(
            string newPassword, string confirmPassword)
        {
            var result = await ConfirmHandler().Handle(
                new ConfirmPasswordResetCommand("RESET_TOKEN", newPassword, confirmPassword),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(PasswordResetErrorCodes.InvalidPassword, result.ErrorCode);
            _store.Verify(s => s.PeekResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _store.Verify(s => s.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-013b
        // EDGE-09: password mới trùng password hiện tại thì từ chối và GIỮ token.
        [Fact]
        [Trait("TC", "TC-UNIT-AuthService-013")]
        [Trait("UC", "UC-08")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ConfirmReset_SamePassword_KeepsToken()
        {
            var user = new UserBuilder().WithPasswordHash("$old").Build();
            _store.Setup(s => s.PeekResetTokenAsync("RESET_TOKEN", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user.Id);
            _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("OldPass1", "$old")).Returns(true);

            var result = await ConfirmHandler().Handle(
                new ConfirmPasswordResetCommand("RESET_TOKEN", "OldPass1", "OldPass1"), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(PasswordResetErrorCodes.PasswordReused, result.ErrorCode);
            Assert.Equal("$old", user.PasswordHash);
            _store.Verify(s => s.ConsumeResetTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
