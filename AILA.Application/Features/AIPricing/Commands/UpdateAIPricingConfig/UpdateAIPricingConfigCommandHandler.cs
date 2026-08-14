using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPricing.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Commands.UpdateAIPricingConfig;

public class UpdateAIPricingConfigCommandHandler : IRequestHandler<UpdateAIPricingConfigCommand, ResponseDto<AIPricingConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAIPricingConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponseDto<AIPricingConfigDto>> Handle(UpdateAIPricingConfigCommand request, CancellationToken cancellationToken)
    {
        if (request.CostPerInputToken < 0 || request.CostPerOutputToken < 0)
            return ResponseDto<AIPricingConfigDto>.FailResult("INVALID_PRICE", "Đơn giá Token không được là số âm.");

        AIApiCostSetting? pricingConfig = null;

        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            pricingConfig = await _unitOfWork.Repository<AIApiCostSetting>().GetByIdAsync(request.Id.Value);

        if (pricingConfig == null)
        {
            var existingList = await _unitOfWork.Repository<AIApiCostSetting>()
                .FindAsync(c => c.ModelId.ToLower() == request.ModelId.ToLower());
            pricingConfig = existingList.FirstOrDefault();
        }

        if (pricingConfig == null)
        {
            pricingConfig = new AIApiCostSetting(
                request.ModelId, request.ServiceName,
                request.CostPerInputToken, request.CostPerOutputToken,
                request.Currency, request.IsActive);
            await _unitOfWork.Repository<AIApiCostSetting>().AddAsync(pricingConfig);
        }
        else
        {
            pricingConfig.UpdatePricing(request.CostPerInputToken, request.CostPerOutputToken, request.IsActive);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ResponseDto<AIPricingConfigDto>.SuccessResult(new AIPricingConfigDto
        {
            Id = pricingConfig.Id,
            ModelId = pricingConfig.ModelId,
            ServiceName = pricingConfig.ServiceName,
            CostPerInputToken = pricingConfig.CostPerInputToken,
            CostPerOutputToken = pricingConfig.CostPerOutputToken,
            Currency = pricingConfig.Currency,
            IsActive = pricingConfig.IsActive
        });
    }
}
