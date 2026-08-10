using MediatR;
using System;

namespace AILA.Application.Features.ExpertSimulations.Commands.StartSimulation;

public record StartSimulationCommand(Guid ExpertId, Guid MaterialId) : IRequest<Guid>;
