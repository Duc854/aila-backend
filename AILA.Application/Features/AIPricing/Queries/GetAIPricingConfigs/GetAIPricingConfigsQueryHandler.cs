using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIPricing.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIPricing.Queries.GetAIPricingConfigs;

public class GetAIPricingConfigsQueryHandler : IRequestHandler<GetAIPricingConfigsQuery, List<AIPricingConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAIPricingConfigsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AIPricingConfigDto>> Handle(GetAIPricingConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();

        if (!configs.Any())
        {
            // Seed default pricing if none exists yet
            var defaultConfig = new AIApiCostSetting(
                "llama-3.3-70b-versatile",
                "Groq",
                costPerInputToken: 0.00000059m,
                costPerOutputToken: 0.00000079m,
                currency: "USD",
                isActive: true);

            await _unitOfWork.Repository<AIApiCostSetting>().AddAsync(defaultConfig);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            configs = new List<AIApiCostSetting> { defaultConfig };
        }

        return configs.Select(c => new AIPricingConfigDto
        {
            Id = c.Id,
            ModelId = c.ModelId,
            ServiceName = c.ServiceName,
            CostPerInputToken = c.CostPerInputToken,
            CostPerOutputToken = c.CostPerOutputToken,
            Currency = c.Currency,
            IsActive = c.IsActive
        }).ToList();
    }
}
