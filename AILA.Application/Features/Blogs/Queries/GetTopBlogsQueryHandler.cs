using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Blogs.DTOs;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Blogs.Queries
{
    public class GetTopBlogsQueryHandler : IRequestHandler<GetTopBlogsQuery, IReadOnlyList<BlogDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTopBlogsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<BlogDto>> Handle(GetTopBlogsQuery request, CancellationToken cancellationToken)
        {
            var blogs = await _unitOfWork.BlogPosts.GetTopBlogsAsync(request.Count);

            return blogs.Select(b => new BlogDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                ThumbnailUrl = b.ThumbnailUrl,
                PublishedAt = b.PublishedAt
            }).ToList().AsReadOnly();
        }
    }
}
