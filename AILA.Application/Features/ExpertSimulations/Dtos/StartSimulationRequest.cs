using System;

namespace AILA.Application.Features.ExpertSimulations.Dtos;

public class StartSimulationRequest
{
    public Guid ExpertId { get; set; }
    public Guid MaterialId { get; set; }
}
