using MediatR;
using System;

namespace AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;

public record DeleteAIPricingConfigCommand(Guid Id) : IRequest<bool>;
