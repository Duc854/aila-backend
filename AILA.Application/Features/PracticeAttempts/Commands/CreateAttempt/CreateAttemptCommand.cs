using MediatR;
namespace AILA.Application.Features.PracticeAttempts.Commands.CreateAttempt;
public record CreateAttemptCommand(Guid EnrollmentId, Guid MaterialId) : IRequest<Guid>;
