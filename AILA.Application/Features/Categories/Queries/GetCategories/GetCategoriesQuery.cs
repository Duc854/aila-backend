using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Queries.GetCategories
{
    /// <summary>
    /// UC-80 - Get Course Categories
    /// </summary>
    public record GetCategoriesQuery()
        : IRequest<ResponseDto<IEnumerable<CategoryDto>>>;
}