using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.UnpublishBlog;

public sealed record UnpublishBlogCommand(Guid BlogId)
    : IRequest<ResponseDto<bool>>;