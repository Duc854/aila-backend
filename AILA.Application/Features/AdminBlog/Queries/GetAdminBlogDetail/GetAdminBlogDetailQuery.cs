using AILA.Application.Common.Dtos;
using AILA.Application.Features.AdminBlog.DTOs;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AdminBlog.Queries.GetAdminBlogDetail
{
    public record GetAdminBlogDetailQuery(Guid BlogId)
        : IRequest<ResponseDto<AdminBlogDto>>;
}