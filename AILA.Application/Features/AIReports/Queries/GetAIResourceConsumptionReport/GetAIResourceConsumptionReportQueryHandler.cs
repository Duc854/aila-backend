using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIReports.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;

public class GetAIResourceConsumptionReportQueryHandler : IRequestHandler<GetAIResourceConsumptionReportQuery, AIResourceConsumptionReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAIResourceConsumptionReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AIResourceConsumptionReportDto> Handle(GetAIResourceConsumptionReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch token logs filtered by date range
        var logs = await _unitOfWork.Repository<AITokenLog>().FindAsync(log =>
            (!request.StartDate.HasValue || log.CreatedAt >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || log.CreatedAt <= request.EndDate.Value));

        var logList = logs.ToList();

        // 2. Fetch AI API pricing settings
        var costSettings = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();
        var pricingDict = costSettings.ToDictionary(c => c.ModelId, StringComparer.OrdinalIgnoreCase);

        // 3. Group by ModelId to calculate statistics and estimated cost per model (BR-02)
        var modelGroups = logList.GroupBy(l => string.IsNullOrWhiteSpace(l.ModelId) ? "llama-3.3-70b-versatile" : l.ModelId);

        var modelBreakdown = new List<AIModelUsageDto>();
        decimal grandTotalCost = 0m;
        long totalPromptTokens = 0;
        long totalCompletionTokens = 0;

        foreach (var group in modelGroups)
        {
            var modelId = group.Key;
            var promptTokens = group.Sum(x => (long)x.PromptTokens);
            var completionTokens = group.Sum(x => (long)x.CompletionTokens);
            var totalTokens = promptTokens + completionTokens;
            var requestCount = group.Count();
            var serviceName = group.FirstOrDefault()?.ServiceType ?? "Groq";

            // Default pricing fallback if model setting is not explicitly configured
            decimal costPerInput = 0.00000059m;  // ~$0.59 per 1M input tokens (Groq Llama 70B)
            decimal costPerOutput = 0.00000079m; // ~$0.79 per 1M output tokens

            if (pricingDict.TryGetValue(modelId, out var customPricing) && customPricing.IsActive)
            {
                costPerInput = customPricing.CostPerInputToken;
                costPerOutput = customPricing.CostPerOutputToken;
                serviceName = customPricing.ServiceName;
            }

            decimal modelCost = (promptTokens * costPerInput) + (completionTokens * costPerOutput);
            grandTotalCost += modelCost;
            totalPromptTokens += promptTokens;
            totalCompletionTokens += completionTokens;

            modelBreakdown.Add(new AIModelUsageDto
            {
                ModelId = modelId,
                ServiceName = serviceName,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                RequestCount = requestCount,
                EstimatedCostUsd = Math.Round(modelCost, 6)
            });
        }

        return new AIResourceConsumptionReportDto
        {
            TotalPromptTokens = totalPromptTokens,
            TotalCompletionTokens = totalCompletionTokens,
            TotalTokens = totalPromptTokens + totalCompletionTokens,
            TotalRequests = logList.Count,
            TotalEstimatedCostUsd = Math.Round(grandTotalCost, 6),
            PeriodStart = request.StartDate,
            PeriodEnd = request.EndDate,
            ModelBreakdown = modelBreakdown.OrderByDescending(m => m.TotalTokens).ToList()
        };
    }
}
