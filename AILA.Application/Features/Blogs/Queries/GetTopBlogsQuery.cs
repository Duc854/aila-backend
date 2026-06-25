using AILA.Application.Features.Blogs.DTOs;
using MediatR;
using System.Collections.Generic;

namespace AILA.Application.Features.Blogs.Queries
{
    public class GetTopBlogsQuery : IRequest<IReadOnlyList<BlogDto>>
    {
        public int Count { get; set; } = 2;
    }
}
