using AILA.Application.Common.Interfaces;
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

        // 2. Xóa Blog
        _uow.BlogPosts.Delete(blog);

        // 3. Lưu
        await _uow.SaveChangesAsync(ct);

        // 4. Trả kết quả
        return ResponseDto<bool>.SuccessResult(true);
    }
}
