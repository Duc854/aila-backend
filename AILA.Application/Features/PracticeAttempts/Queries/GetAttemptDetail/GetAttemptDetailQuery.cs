using MediatR;
using AILA.Application.Common.Dtos.AI;
namespace AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;
public record GetAttemptDetailQuery(Guid AttemptId, Guid RequestAccountId = default) : IRequest<PracticeAttemptDto>;
