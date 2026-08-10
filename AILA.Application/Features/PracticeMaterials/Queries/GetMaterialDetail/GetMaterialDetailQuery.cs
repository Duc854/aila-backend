// Application/Features/PracticeMaterials/Queries/GetMaterialDetail/GetMaterialDetailQuery.cs
using AILA.Application.Common.Dtos.AI;
using MediatR;

namespace AILA.Application.Features.PracticeMaterials.Queries.GetMaterialDetail;

public record GetMaterialDetailQuery(Guid Id) : IRequest<AIPracticeMaterialDetailDto>;
