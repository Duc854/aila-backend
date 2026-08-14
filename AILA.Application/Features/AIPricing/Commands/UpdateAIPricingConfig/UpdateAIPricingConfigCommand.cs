using AILA.Application.Features.AIPricing.Dtos;
using MediatR;
using Shared.Wrappers;
using System;

namespace AILA.Application.Features.AIPricing.Commands.UpdateAIPricingConfig;

public record UpdateAIPricingConfigCommand(
    Guid? Id,
    string ModelId,
    string ServiceName,
    decimal CostPerInputToken,
    decimal CostPerOutputToken,
    string Currency = "USD",
    bool IsActive = true) : IRequest<ResponseDto<AIPricingConfigDto>>;
