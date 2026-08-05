using MediatR;
using Shared.Wrappers;
using AILA.Application.Features.AdminBlog.DTOs;

namespace AILA.Application.Features.AdminBlog.Commands.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string Slug,
    string Content,
    string? ThumbnailUrl)
    : IRequest<ResponseDto<AdminBlogDto>>;
