using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Queries.GetAdminBlogDetail
{
    public class GetAdminBlogDetailQueryHandler
        : IRequestHandler<GetAdminBlogDetailQuery, ResponseDto<AdminBlogDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAdminBlogDetailQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<AdminBlogDto>> Handle(
            GetAdminBlogDetailQuery request,
            CancellationToken cancellationToken)
        {
            var blog = await _unitOfWork.BlogPosts.GetBlogDetailAsync(
                request.BlogId,
                cancellationToken);

            if (blog == null)
            {
                return ResponseDto<AdminBlogDto>.FailResult(
                    "BLOG_NOT_FOUND",
                    "Blog does not exist.");
            }

            return ResponseDto<AdminBlogDto>.SuccessResult(
                blog.MapToDto());
        }
    }
}
