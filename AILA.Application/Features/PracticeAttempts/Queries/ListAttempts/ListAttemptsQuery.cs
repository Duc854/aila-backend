using MediatR;
using AILA.Application.Common.Dtos.AI;
namespace AILA.Application.Features.PracticeAttempts.Queries.ListAttempts;
public record ListAttemptsQuery(Guid EnrollmentId, Guid RequestAccountId = default) : IRequest<List<PracticeAttemptDto>>;
