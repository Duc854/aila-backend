using MediatR;
using Shared.Wrappers;
using System;

namespace AILA.Application.Features.AIPricing.Commands.DeleteAIPricingConfig;

public record DeleteAIPricingConfigCommand(Guid Id) : IRequest<ResponseDto<bool>>;
