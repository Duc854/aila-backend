using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Commands.PublishBlog;

public sealed class PublishBlogCommandHandler
    : IRequestHandler<PublishBlogCommand, ResponseDto<bool>>
{
    private readonly IUnitOfWork _uow;

    public PublishBlogCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<bool>> Handle(
        PublishBlogCommand request,
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

        // 2. Publish
        blog.Publish();

        // 3. Lưu
        await _uow.SaveChangesAsync(ct);

        // 4. Thành công
        return ResponseDto<bool>.SuccessResult(true);
    }
}
