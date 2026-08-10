using MediatR;
using AILA.Application.Common.Dtos.AI;
namespace AILA.Application.Features.PracticeAttempts.Queries.ListAttempts;
public record ListAttemptsQuery(Guid EnrollmentId) : IRequest<List<PracticeAttemptDto>>;
