using AILA.Application.Features.AIPricing.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;

public record GetAIPricingConfigsQuery() : IRequest<ResponseDto<AIPricingListResponseDto>>;
