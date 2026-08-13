using AILA.Application.Features.AIPricing.Dtos;
using MediatR;

namespace AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;

public record CreateAIPricingConfigCommand(
    string ModelId,
    string ServiceName,
    decimal CostPerInputToken,
    decimal CostPerOutputToken,
    string Currency = "USD",
    bool IsActive = true
) : IRequest<AIPricingConfigDto>;
