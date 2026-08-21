using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.DeleteBlog;

public sealed class DeleteBlogCommandHandler
    : IRequestHandler<DeleteBlogCommand, ResponseDto<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteBlogCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<bool>> Handle(
        DeleteBlogCommand request,
        CancellationToken ct)
    {
        // 1. Kiểm tra Blog tồn tại
        var blog = await _uow.BlogPosts.GetByIdAsync(request.BlogId);

        if (blog == null)
        {
            return ResponseDto<bool>.FailResult(
                "BLOG_NOT_FOUND",
                "Không tìm thấy bài viết.");
        }

        await _uow.BeginTransactionAsync(ct);

        try
        {
            // 2. Xóa Blog
            _uow.BlogPosts.Delete(blog);

            // 3. Ghi Audit Log nếu có AdminId
            if (request.AdminId != Guid.Empty && _uow.AdminActivityLogs != null)
            {
                var activityLog = new AdminActivityLog(
                    request.AdminId,
                    AdminAction.Delete,
                    $"Xóa bài viết blog: {blog.Title}");

                await _uow.AdminActivityLogs.AddAsync(activityLog);
            }

            await _uow.CommitTransactionAsync(ct);

            // 4. Trả kết quả
            return ResponseDto<bool>.SuccessResult(true);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
