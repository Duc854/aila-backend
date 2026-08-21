using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
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

        await _uow.BeginTransactionAsync(ct);

        try
        {
            // 2. Tạo Blog
            var blog = new BlogPost(
                request.Title,
                request.Slug,
                request.Content,
                request.ThumbnailUrl);

            // 3. Lưu
            await _uow.BlogPosts.AddAsync(blog);

            // 4. Ghi Audit Log nếu có AdminId
            if (request.AdminId != Guid.Empty && _uow.AdminActivityLogs != null)
            {
                var activityLog = new AdminActivityLog(
                    request.AdminId,
                    AdminAction.Create,
                    $"Tạo bài viết blog mới: {blog.Title}");

                await _uow.AdminActivityLogs.AddAsync(activityLog);
            }

            await _uow.CommitTransactionAsync(ct);

            // 5. Trả DTO
            return ResponseDto<AdminBlogDto>
                .SuccessResult(blog.MapToDto());
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
