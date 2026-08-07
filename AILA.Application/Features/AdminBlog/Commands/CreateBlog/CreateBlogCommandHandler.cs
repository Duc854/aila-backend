using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.CreateBlog;

public sealed class CreateBlogCommandHandler
    : IRequestHandler<CreateBlogCommand, ResponseDto<AdminBlogDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateBlogCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<AdminBlogDto>> Handle(
        CreateBlogCommand request,
        CancellationToken ct)
    {
        // 1. Kiểm tra Slug đã tồn tại
        var slugExists = await _uow.BlogPosts.ExistsSlugAsync(
            request.Slug,
            null,
            ct);

        if (slugExists)
        {
            return ResponseDto<AdminBlogDto>.FailResult(
                "BLOG_SLUG_EXISTS",
                "Slug đã tồn tại.");
        }

        // 2. Tạo Blog
        var blog = new BlogPost(
            request.Title,
            request.Slug,
            request.Content,
            request.ThumbnailUrl);

        // 3. Lưu
        await _uow.BlogPosts.AddAsync(blog);

        await _uow.SaveChangesAsync(ct);

        // 4. Trả DTO
        return ResponseDto<AdminBlogDto>
            .SuccessResult(blog.MapToDto());
    }
}
