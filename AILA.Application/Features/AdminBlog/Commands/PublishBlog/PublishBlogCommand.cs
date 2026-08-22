using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.PublishBlog;

public sealed record PublishBlogCommand(Guid BlogId, Guid AdminId = default)
    : IRequest<ResponseDto<bool>>;
