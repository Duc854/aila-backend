using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Profile.Commands.UploadAvatar
{
    public record UploadAvatarCommand(
        Guid UserId,
        Stream FileStream,
        string FileName,
        string ContentType,
        long FileSizeBytes
    ) : IRequest<ResponseDto<UploadAvatarDto>>;

    public record UploadAvatarDto(string AvatarUrl);
}
