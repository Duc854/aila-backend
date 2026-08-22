using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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

        await _uow.BeginTransactionAsync(ct);

        try
        {
            // 3. Cập nhật Blog
            blog.UpdateContent(
                request.Title,
                request.Slug,
                request.Content,
                request.ThumbnailUrl);

            // 4. Ghi Audit Log nếu có AdminId
            if (request.AdminId != Guid.Empty && _uow.AdminActivityLogs != null)
            {
                var activityLog = new AdminActivityLog(
                    request.AdminId,
                    AdminAction.Update,
                    $"Cập nhật bài viết blog: {blog.Title}");

                await _uow.AdminActivityLogs.AddAsync(activityLog);
            }

            await _uow.CommitTransactionAsync(ct);

            // 5. Trả DTO
            return ResponseDto<AdminBlogDto>
                .SuccessResult(
                    blog.MapToDto());
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
