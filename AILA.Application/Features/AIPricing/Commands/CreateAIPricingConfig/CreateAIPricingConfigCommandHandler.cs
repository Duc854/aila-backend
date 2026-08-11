using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPricing.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Commands.CreateAIPricingConfig;

public class CreateAIPricingConfigCommandHandler : IRequestHandler<CreateAIPricingConfigCommand, AIPricingConfigDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAIPricingConfigCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AIPricingConfigDto> Handle(CreateAIPricingConfigCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ArgumentException("ModelId không được để trống.", nameof(request.ModelId));
        }

        if (request.CostPerInputToken < 0 || request.CostPerOutputToken < 0)
        {
            throw new ArgumentException("Đơn giá Token không được là số âm.");
        }

        // Check if modelId already exists
        var existingList = await _unitOfWork.Repository<AIApiCostSetting>()
            .FindAsync(c => c.ModelId.ToLower() == request.ModelId.Trim().ToLower());

        if (existingList.Any())
        {
            throw new BusinessRuleException($"Cấu hình giá cho Model '{request.ModelId}' đã tồn tại trong hệ thống. Vui lòng cập nhật thay vì tạo mới.");
        }

        var newSetting = new AIApiCostSetting(
            request.ModelId.Trim(),
            request.ServiceName?.Trim() ?? "Groq",
            request.CostPerInputToken,
            request.CostPerOutputToken,
            string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim(),
            request.IsActive);

        await _unitOfWork.Repository<AIApiCostSetting>().AddAsync(newSetting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AIPricingConfigDto
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
        };
    }
}
