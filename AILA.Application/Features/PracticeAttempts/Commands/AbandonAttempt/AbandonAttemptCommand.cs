using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Commands.AbandonAttempt;
public record AbandonAttemptCommand(Guid AttemptId) : IRequest<Unit>;
