using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.CreateCategory
{
    /// <summary>
    /// UC-81 - Create Course Category
    /// </summary>
    public record CreateCategoryCommand(
        string Name,
        string? Description,
        int OrderIndex
    ) : IRequest<ResponseDto<CategoryDto>>;
}
