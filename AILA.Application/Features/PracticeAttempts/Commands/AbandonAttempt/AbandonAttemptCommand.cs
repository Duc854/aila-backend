using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;
public record AbandonAttemptCommand(Guid AttemptId, Guid RequestAccountId = default) : IRequest<Unit>;
