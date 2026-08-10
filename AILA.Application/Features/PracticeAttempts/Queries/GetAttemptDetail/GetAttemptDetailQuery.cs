using MediatR;
using AILA.Application.Common.Dtos.AI;
namespace AILA.Application.Features.PracticeAttempts.Queries.GetAttemptDetail;
public record GetAttemptDetailQuery(Guid AttemptId) : IRequest<PracticeAttemptDto>;
