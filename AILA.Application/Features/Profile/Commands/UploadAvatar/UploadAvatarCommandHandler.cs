using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UploadAvatar
{
    public class UploadAvatarCommandHandler(IUnitOfWork uow, IFileStorageService fileStorage)
        : IRequestHandler<UploadAvatarCommand, ResponseDto<UploadAvatarDto>>
    {
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public async Task<ResponseDto<UploadAvatarDto>> Handle(UploadAvatarCommand request, CancellationToken ct)
        {
            if (request.FileSizeBytes <= 0)
                return ResponseDto<UploadAvatarDto>.FailResult("VALIDATION_ERROR", "File ảnh không được để trống.");

            if (request.FileSizeBytes > MaxFileSizeBytes)
                return ResponseDto<UploadAvatarDto>.FailResult("VALIDATION_ERROR", "File ảnh vượt quá dung lượng cho phép (tối đa 5MB).");

            if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
                return ResponseDto<UploadAvatarDto>.FailResult("VALIDATION_ERROR", "Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPEG, PNG hoặc WEBP.");

            var user = await uow.Users.GetByIdAsync(request.UserId);
            if (user == null)
                return ResponseDto<UploadAvatarDto>.FailResult("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            if (!user.IsActive)
                return ResponseDto<UploadAvatarDto>.FailResult("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa.");

            var avatarUrl = await fileStorage.UploadImageAsync(request.FileStream, request.FileName, "avatars", ct);

            user.UpdateProfile(user.FullName, avatarUrl);
            await uow.SaveChangesAsync(ct);

            return ResponseDto<UploadAvatarDto>.SuccessResult(new UploadAvatarDto(avatarUrl));
        }
    }
}
