using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.ReorderCategories
{
    /// <summary>
    /// UC-84 - Reorder Course Categories
    /// </summary>
    public record ReorderCategoriesCommand(
        List<Guid> CategoryIds
    ) : IRequest<ResponseDto<object>>;
}