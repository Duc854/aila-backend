using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.DeleteBlog;

public sealed record DeleteBlogCommand(Guid BlogId, Guid AdminId = default)
    : IRequest<ResponseDto<bool>>;
