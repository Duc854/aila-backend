using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.UpdateBlog;

public sealed class UpdateBlogCommandHandler
    : IRequestHandler<UpdateBlogCommand, ResponseDto<AdminBlogDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateBlogCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<AdminBlogDto>> Handle(
        UpdateBlogCommand request,
        CancellationToken ct)
    {
        // 1. Kiểm tra Blog tồn tại
        var blog = await _uow.BlogPosts.GetByIdAsync(request.BlogId);

        if (blog == null)
        {
            return ResponseDto<AdminBlogDto>
                .FailResult(
                    "BLOG_NOT_FOUND",
                    "Không tìm thấy bài viết.");
        }

        // 2. Kiểm tra Slug đã tồn tại
        var slugExists = await _uow.BlogPosts.ExistsSlugAsync(
            request.Slug,
            request.BlogId,
            ct);

        if (slugExists)
        {
            return ResponseDto<AdminBlogDto>
                .FailResult(
                    "BLOG_SLUG_EXISTS",
                    "Slug đã tồn tại.");
        }

        // 3. Cập nhật Blog
        blog.UpdateContent(
            request.Title,
            request.Slug,
            request.Content,
            request.ThumbnailUrl);

        // 4. Lưu
        await _uow.SaveChangesAsync(ct);

        // 5. Trả DTO
        return ResponseDto<AdminBlogDto>
            .SuccessResult(
                blog.MapToDto());
    }
}
