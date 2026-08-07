using AILA.Application.Common.Dtos.AI;
using MediatR;
using System;

namespace AILA.Application.Features.ExpertSimulations.Commands.SubmitSimulationPrompt;

public record SubmitSimulationPromptCommand(
    Guid SimulationAttemptId,
    string UserPrompt
) : IRequest<PromptSubmissionDto>;
