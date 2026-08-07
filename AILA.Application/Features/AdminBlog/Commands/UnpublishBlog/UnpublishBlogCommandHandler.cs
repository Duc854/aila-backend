using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.UnpublishBlog;

public sealed class UnpublishBlogCommandHandler
    : IRequestHandler<UnpublishBlogCommand, ResponseDto<bool>>
{
    private readonly IUnitOfWork _uow;

    public UnpublishBlogCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<bool>> Handle(
        UnpublishBlogCommand request,
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

        // 2. Unpublish
        blog.Unpublish();

        // 3. Lưu
        await _uow.SaveChangesAsync(ct);

        // 4. Thành công
        return ResponseDto<bool>.SuccessResult(true);
    }
}
