using AILA.Application.Features.Categories.DTOs;
using MediatR;
using System.Collections.Generic;

namespace AILA.Application.Features.Categories.Queries
{
    public class GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>
    {
    }
}
