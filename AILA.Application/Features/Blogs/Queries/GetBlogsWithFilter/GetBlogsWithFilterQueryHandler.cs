using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Blogs.Queries.GetBlogsWithFilter
{
    public class GetBlogsWithFilterQueryHandler : IRequestHandler<GetBlogsWithFilterQuery, ResponseDto<PageResult<BlogPostDto>>>
    {
        private readonly IUnitOfWork _uow;

        public GetBlogsWithFilterQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<PageResult<BlogPostDto>>> Handle(GetBlogsWithFilterQuery request, CancellationToken cancellationToken)
        {
            // Gọi tầng Infrastructure xử lý filter và phân trang ở mức database
            var (blogs, totalCount) = await _uow.BlogPosts.GetPagedBlogsAsync(
                request.Search,
                request.PageRequest.PageIndex,
                request.PageRequest.PageSize,
                cancellationToken);

            // Ánh xạ sang cấu trúc DTO an toàn cho giao diện hiển thị
            var blogDtos = blogs.Select(b => new BlogPostDto(
                b.Id,
                b.Title,
                b.Slug,
                b.ThumbnailUrl,
                "Admin",
                b.CreatedAt
            )).ToList();

            // Đóng gói phân trang kết quả trả về
            var pagedData = new PageResult<BlogPostDto>(blogDtos, totalCount, request.PageRequest.PageIndex, request.PageRequest.PageSize);

            return ResponseDto<PageResult<BlogPostDto>>.SuccessResult(pagedData);
        }
    }
}
