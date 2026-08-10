using AILA.Application.Common.Dtos.AI;
using MediatR;

namespace AILA.Application.Features.ExpertSimulations.Queries.GetSimulationDetail;

/// <summary>
/// UC-60: Lấy chi tiết một phiên thử nghiệm simulation của Expert.
/// </summary>
public sealed record GetSimulationDetailQuery(Guid SimulationId) : IRequest<PracticeAttemptDto>;
