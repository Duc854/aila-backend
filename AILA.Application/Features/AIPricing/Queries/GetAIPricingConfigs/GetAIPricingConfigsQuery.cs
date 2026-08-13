using AILA.Application.Features.AIPricing.Dtos;
using MediatR;

namespace AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;

public record GetAIPricingConfigsQuery() : IRequest<AIPricingListResponseDto>;
