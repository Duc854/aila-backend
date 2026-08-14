using AILA.Application.Features.AIPricing.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;

public record CreateAIPricingConfigCommand(
    string ModelId,
    string ServiceName,
    decimal CostPerInputToken,
    decimal CostPerOutputToken,
    string Currency = "USD",
    bool IsActive = true
) : IRequest<ResponseDto<AIPricingConfigDto>>;
