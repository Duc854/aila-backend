using MediatR;
using AILA.Application.Common.Dtos.AI;
namespace AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
public record SubmitPromptCommand(Guid AttemptId, string UserPrompt) : IRequest<PromptSubmissionDto>;
