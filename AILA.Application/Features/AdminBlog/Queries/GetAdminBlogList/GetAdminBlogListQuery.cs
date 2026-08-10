using AILA.Application.Common.Dtos;
using AILA.Application.Features.AdminBlog.DTOs;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Queries.GetAdminBlogList
{
    public record GetAdminBlogListQuery(
        string? Search,
        int PageNumber = 1,
        int PageSize = 10)
        : IRequest<ResponseDto<AdminBlogPagedResultDto>>;
}
