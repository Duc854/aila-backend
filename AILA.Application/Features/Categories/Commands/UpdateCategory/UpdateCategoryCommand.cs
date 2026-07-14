using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.UpdateCategory
{
    /// <summary>
    /// UC-82 - Update Course Category
    /// </summary>
    public record UpdateCategoryCommand(
        Guid CategoryId,
        string Name,
        string? Description
    ) : IRequest<ResponseDto<CategoryDto>>;
}