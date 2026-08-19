using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
public record CreateAttemptCommand(Guid EnrollmentId, Guid MaterialId, Guid RequestAccountId = default) : IRequest<Guid>;
