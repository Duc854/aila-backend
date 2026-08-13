using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIReports.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIReports.Queries.GetAIConsumptionTrend;

public class GetAIConsumptionTrendQueryHandler : IRequestHandler<GetAIConsumptionTrendQuery, AIConsumptionTrendResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private const decimal ExchangeRate = 25400m;

    public GetAIConsumptionTrendQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AIConsumptionTrendResponseDto> Handle(GetAIConsumptionTrendQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch token logs
        var logs = await _unitOfWork.Repository<AITokenLog>().FindAsync(log =>
            (!request.StartDate.HasValue || log.CreatedAt >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || log.CreatedAt <= request.EndDate.Value));

        var logList = logs.ToList();

        // 2. Fetch pricing settings
        var costSettings = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();
        var pricingDict = costSettings.ToDictionary(c => c.ModelId, StringComparer.OrdinalIgnoreCase);

        // Fallback pricing
        const decimal defaultCostInput = 0.00000059m;
        const decimal defaultCostOutput = 0.00000079m;

        // Group by Date based on Interval ("day", "week", "month")
        var isMonth = string.Equals(request.Interval, "month", StringComparison.OrdinalIgnoreCase);

        var dateGroups = logList
            .GroupBy(l => isMonth ? l.CreatedAt.ToString("yyyy-MM") : l.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key);

        var dataPoints = new List<AIConsumptionTrendPointDto>();
        long totalAllTokens = 0;
        decimal totalAllCostUsd = 0m;

        foreach (var group in dateGroups)
        {
            var dateKey = group.Key;
            long promptTokens = 0;
            long completionTokens = 0;
            decimal dayCostUsd = 0m;

            foreach (var log in group)
            {
                long pTokens = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.PromptTokens : 220L;
                long cTokens = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.CompletionTokens : 160L;

                promptTokens += pTokens;
                completionTokens += cTokens;

                var modelId = string.IsNullOrWhiteSpace(log.ModelId) ? "llama-3.3-70b-versatile" : log.ModelId;
                decimal costInput = defaultCostInput;
                decimal costOutput = defaultCostOutput;

                if (pricingDict.TryGetValue(modelId, out var pricing) && pricing.IsActive)
                {
                    costInput = pricing.CostPerInputToken;
                    costOutput = pricing.CostPerOutputToken;
                }

                dayCostUsd += (pTokens * costInput) + (cTokens * costOutput);
            }

            var groupTotalTokens = promptTokens + completionTokens;
            totalAllTokens += groupTotalTokens;
            totalAllCostUsd += dayCostUsd;

            dataPoints.Add(new AIConsumptionTrendPointDto
            {
                Date = dateKey,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = groupTotalTokens,
                TotalRequests = group.Count(),
                EstimatedCostUsd = Math.Round(dayCostUsd, 6),
                EstimatedCostVnd = Math.Round(dayCostUsd * ExchangeRate, 0)
            });
        }

        return new AIConsumptionTrendResponseDto
        {
            Interval = request.Interval ?? "day",
            PeriodStart = request.StartDate,
            PeriodEnd = request.EndDate,
            TotalTokens = totalAllTokens,
            TotalEstimatedCostUsd = Math.Round(totalAllCostUsd, 6),
            TotalEstimatedCostVnd = Math.Round(totalAllCostUsd * ExchangeRate, 0),
            DataPoints = dataPoints
        };
    }
}
