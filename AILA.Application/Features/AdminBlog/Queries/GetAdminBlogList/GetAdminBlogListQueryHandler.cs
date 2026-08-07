using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AdminBlog.DTOs;
using AILA.Application.Features.AdminBlog.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Queries.GetAdminBlogList
{
    public class GetAdminBlogListQueryHandler
        : IRequestHandler<GetAdminBlogListQuery, ResponseDto<AdminBlogPagedResultDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAdminBlogListQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<AdminBlogPagedResultDto>> Handle(
            GetAdminBlogListQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount)
                = await _unitOfWork.BlogPosts.GetPagedAdminBlogsAsync(
                    request.Search,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

            var result = new AdminBlogPagedResultDto
            {
                Items = items.MapToListItemDtos(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return ResponseDto<AdminBlogPagedResultDto>
                .SuccessResult(result);
        }
    }
}
