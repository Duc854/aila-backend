using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPricing.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;

public class GetAIPricingConfigsQueryHandler : IRequestHandler<GetAIPricingConfigsQuery, ResponseDto<AIPricingListResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAIPricingConfigsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponseDto<AIPricingListResponseDto>> Handle(GetAIPricingConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();
        var configList = configs.OrderByDescending(c => c.IsActive).ThenBy(c => c.ModelId).ToList();

        var isConfigured = configList.Any();

        var items = configList.Select(c => new AIPricingConfigDto
        {
            Id = c.Id,
            ModelId = c.ModelId,
            ServiceName = c.ServiceName,
            CostPerInputToken = c.CostPerInputToken,
            CostPerOutputToken = c.CostPerOutputToken,
            Currency = c.Currency,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        var defaultModel = configList.FirstOrDefault(c => c.IsActive)?.ModelId ?? "llama-3.3-70b-versatile";

        return ResponseDto<AIPricingListResponseDto>.SuccessResult(new AIPricingListResponseDto
        {
            IsConfigured = isConfigured,
            DefaultModelId = defaultModel,
            ExchangeRateUsdToVnd = 25400m,
            Items = items
        });
    }
}
