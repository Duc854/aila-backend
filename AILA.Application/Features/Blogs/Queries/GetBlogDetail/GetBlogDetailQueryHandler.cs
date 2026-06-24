using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Blogs.Dtos;
using MediatR;

namespace AILA.Application.Features.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQueryHandler
        : IRequestHandler<GetBlogDetailQuery, BlogDetailDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetBlogDetailQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BlogDetailDto?> Handle(
            GetBlogDetailQuery request,
            CancellationToken cancellationToken)
        {
            var blog = await _uow.BlogPosts.GetBlogDetailAsync(request.Id, cancellationToken);

            if (blog == null || !blog.IsPublished)
                return null;

            // Tăng ViewCount nguyên tử để tránh race condition (EC06)
            await _uow.BlogPosts.IncrementViewCountAsync(request.Id, cancellationToken);

            var relatedBlogs = await _uow.BlogPosts
                .GetRelatedBlogsAsync(request.Id, 5, cancellationToken);

            return new BlogDetailDto(
                blog.Id,
                blog.Title,
                blog.Content,
                blog.ThumbnailUrl,
                AuthorName: "Admin",
                ViewCount: blog.ViewCount + 1,
                blog.CreatedAt,
                blog.PublishedAt,
                relatedBlogs.Select(r => new RelatedBlogDto(
                    r.Id,
                    r.Title,
                    r.ThumbnailUrl,
                    r.PublishedAt
                )).ToList()
            );
        }
    }
}
