using AILA.Application.Common.Dtos.AI;
using MediatR;
using System;

namespace AILA.Application.Features.ExpertSimulations.Commands.CompleteSimulation;

public record CompleteSimulationCommand(Guid SimulationSessionId) : IRequest<CompleteAttemptResponseDto>;
