using AILA.Application.Common.Dtos;
using MediatR;

namespace AILA.Application.Features.Categories.Queries
{
    public record GetActiveCategoriesQuery() : IRequest<List<CategoryDto>>;
}
