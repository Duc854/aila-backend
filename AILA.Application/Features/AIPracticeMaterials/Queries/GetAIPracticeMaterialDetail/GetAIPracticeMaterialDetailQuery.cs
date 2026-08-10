using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterials.Queries.GetAIPracticeMaterialDetail
{
    public sealed record GetAIPracticeMaterialDetailQuery(
        Guid ExpertId,
        Guid MaterialId)
        : IRequest<ResponseDto<AIPracticeMaterialDetailDto>>;
}
