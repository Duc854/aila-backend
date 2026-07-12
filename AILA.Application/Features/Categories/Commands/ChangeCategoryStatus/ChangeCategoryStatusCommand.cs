using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Commands.ChangeCategoryStatus
{
    /// <summary>
    /// UC-83 - Change Course Category Status
    /// </summary>
    public record ChangeCategoryStatusCommand(
        Guid CategoryId,
        bool IsActive
    ) : IRequest<ResponseDto<CategoryDto>>;
}