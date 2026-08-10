using AILA.Application.Features.AIPricing.Dtos;
using MediatR;
using System.Collections.Generic;

namespace AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;

public record GetAIPricingConfigsQuery() : IRequest<List<AIPricingConfigDto>>;
