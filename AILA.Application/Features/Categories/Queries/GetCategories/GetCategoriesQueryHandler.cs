using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Categories.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Categories.Queries.GetCategories
{
    /// <summary>
    /// UC-80 - Get Course Categories
    /// </summary>
    public class GetCategoriesQueryHandler(IUnitOfWork uow)
        : IRequestHandler<GetCategoriesQuery, ResponseDto<IEnumerable<CategoryDto>>>
    {
        public async Task<ResponseDto<IEnumerable<CategoryDto>>> Handle(
            GetCategoriesQuery request,
            CancellationToken ct)
        {
            var categories = await uow.Categories.GetAllOrderedAsync(ct);

            var result = categories.Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.OrderIndex,
                c.IsActive
            ));

            return ResponseDto<IEnumerable<CategoryDto>>
                .SuccessResult(result);
        }
    }
}
