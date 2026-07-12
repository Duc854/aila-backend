using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Blogs.Queries.GetTopBlogs
{
    public class GetTopBlogsQuery : IRequest<ResponseDto<List<TopBlogResponse>>>
    {
        public int Count { get; set; } = 2;
    }

    public class TopBlogResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class GetTopBlogsQueryHandler : IRequestHandler<GetTopBlogsQuery, ResponseDto<List<TopBlogResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTopBlogsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<TopBlogResponse>>> Handle(GetTopBlogsQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.BlogPosts.GetPagedBlogsAsync(
                search: null,
                pageNumber: 1,
                pageSize: request.Count,
                cancellationToken: cancellationToken);

            var blogs = result.Items.Select(b => new TopBlogResponse
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                ThumbnailUrl = b.ThumbnailUrl,
                PublishedAt = b.PublishedAt
            }).ToList();

            return ResponseDto<List<TopBlogResponse>>.SuccessResult(blogs);
        }
    }
}
