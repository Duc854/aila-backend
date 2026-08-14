using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPricing.Dtos;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;

public class CreateAIPricingConfigCommandHandler : IRequestHandler<CreateAIPricingConfigCommand, ResponseDto<AIPricingConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAIPricingConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResponseDto<AIPricingConfigDto>> Handle(CreateAIPricingConfigCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ModelId))
            return ResponseDto<AIPricingConfigDto>.FailResult("INVALID_MODEL_ID", "ModelId không được để trống.");

        if (request.CostPerInputToken < 0 || request.CostPerOutputToken < 0)
            return ResponseDto<AIPricingConfigDto>.FailResult("INVALID_PRICE", "Đơn giá Token không được là số âm.");

        var existingList = await _unitOfWork.Repository<AIApiCostSetting>()
            .FindAsync(c => c.ModelId.ToLower() == request.ModelId.Trim().ToLower());

        if (existingList.Any())
            return ResponseDto<AIPricingConfigDto>.FailResult("DUPLICATE_MODEL",
                $"Cấu hình giá cho Model '{request.ModelId}' đã tồn tại. Vui lòng cập nhật thay vì tạo mới.");

        var newSetting = new AIApiCostSetting(
            request.ModelId.Trim(),
            request.ServiceName?.Trim() ?? "Groq",
            request.CostPerInputToken,
            request.CostPerOutputToken,
            string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim(),
            request.IsActive);

        await _unitOfWork.Repository<AIApiCostSetting>().AddAsync(newSetting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ResponseDto<AIPricingConfigDto>.SuccessResult(new AIPricingConfigDto
        {
            Id = newSetting.Id,
            ModelId = newSetting.ModelId,
            ServiceName = newSetting.ServiceName,
            CostPerInputToken = newSetting.CostPerInputToken,
            CostPerOutputToken = newSetting.CostPerOutputToken,
            Currency = newSetting.Currency,
            IsActive = newSetting.IsActive,
            CreatedAt = newSetting.CreatedAt,
            UpdatedAt = newSetting.UpdatedAt
        });
    }
}
