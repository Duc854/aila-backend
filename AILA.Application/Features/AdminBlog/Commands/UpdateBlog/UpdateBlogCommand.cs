using AILA.Application.Features.AdminBlog.DTOs;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid BlogId,
    string Title,
    string Slug,
    string Content,
    string? ThumbnailUrl)
    : IRequest<ResponseDto<AdminBlogDto>>;
