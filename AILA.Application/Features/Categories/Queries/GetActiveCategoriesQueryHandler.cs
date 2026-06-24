using AILA.Application.Common.Dtos;
using AILA.Application.Common.Interfaces;
using MediatR;

namespace AILA.Application.Features.Categories.Queries
{
    public class GetActiveCategoriesQueryHandler
        : IRequestHandler<GetActiveCategoriesQuery, List<CategoryDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetActiveCategoriesQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<CategoryDto>> Handle(
            GetActiveCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            var categories = await _uow.Categories.GetActiveCategoriesAsync();

            return categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                OrderIndex = c.OrderIndex
            }).ToList();
        }
    }
}
