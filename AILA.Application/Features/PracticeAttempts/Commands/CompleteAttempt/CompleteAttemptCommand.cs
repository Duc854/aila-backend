using AILA.Application.Common.Dtos.AI;
using MediatR;

namespace AILA.Application.Features.PracticeAttempts.Commands.CompleteAttempt;

public record CompleteAttemptCommand(Guid AttemptId, Guid RequestAccountId = default) : IRequest<CompleteAttemptResponseDto>;
