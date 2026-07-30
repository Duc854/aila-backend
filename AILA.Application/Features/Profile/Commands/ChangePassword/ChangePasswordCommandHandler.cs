using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler(IUnitOfWork uow, IPasswordHasher passwordHasher)
        : IRequestHandler<ChangePasswordCommand, ResponseDto<object>>
    {
        public async Task<ResponseDto<object>> Handle(ChangePasswordCommand request, CancellationToken ct)
        {
            // --- Input validation ---
            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return ResponseDto<object>.FailResult("VALIDATION_ERROR", "Mật khẩu mới không được để trống.");

            if (!IsPasswordStrong(request.NewPassword))
                return ResponseDto<object>.FailResult("VALIDATION_ERROR", "Mật khẩu mới phải có ít nhất 8 ký tự.");

            // --- Load user ---
            var user = await uow.Repository<AILA.Domain.Entities.User>().GetByIdAsync(request.UserId);

            if (user == null)
                return ResponseDto<object>.FailResult("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            if (!user.IsActive)
                return ResponseDto<object>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            bool isFirstTime = user.PasswordHash == null;

            // --- Verify current password if user already has one ---
            if (!isFirstTime)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                    return ResponseDto<object>.FailResult("VALIDATION_ERROR", "Mật khẩu hiện tại không được để trống.");

                if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash!))
                    return ResponseDto<object>.FailResult("WRONG_PASSWORD", "Mật khẩu hiện tại không chính xác.");

                if (passwordHasher.Verify(request.NewPassword, user.PasswordHash!))
                    return ResponseDto<object>.FailResult("SAME_PASSWORD", "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            // --- Hash and save new password ---
            var newHash = passwordHasher.HashPassword(request.NewPassword);
            user.UpdatePassword(newHash);

            await uow.SaveChangesAsync(ct);

            return ResponseDto<object>.SuccessResult(null!);
        }

        // Policy đổi mật khẩu: chỉ yêu cầu tối thiểu 8 ký tự.
        private static bool IsPasswordStrong(string password)
            => password.Length >= 8;
    }
}
