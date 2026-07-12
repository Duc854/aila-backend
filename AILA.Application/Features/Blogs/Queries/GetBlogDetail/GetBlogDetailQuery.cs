using AILA.Application.Features.Blogs.Dtos;
using MediatR;

namespace AILA.Application.Features.Blogs.Queries.GetBlogDetail
{
    public record GetBlogDetailQuery(Guid Id) : IRequest<BlogDetailDto?>;
}
