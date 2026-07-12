using AILA.Application.Common.Dtos;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Materials.Queries.GetMaterialDetail
{
    public record GetMaterialDetailQuery(Guid CourseId, Guid MaterialId) : IRequest<ResponseDto<MaterialDetailDto>>;
}
