using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPracticeMaterial.CreateAIPracticeMaterials
{
    public sealed record CreateAIPracticeMaterialCommand(
        Guid ExpertId,
        CreateAIPracticeMaterialRequestDto Request)
        : IRequest<ResponseDto<AIPracticeMaterialDto>>;
}
