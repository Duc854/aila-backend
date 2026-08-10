using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Materials.Commands.MarkMaterialAsCompleted
{
    public record MarkMaterialAsCompletedCommand(Guid CourseId, Guid MaterialId, Guid LearnerId)
            : IRequest<ResponseDto<bool>>;
}
