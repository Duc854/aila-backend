using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIReports.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIReports.Queries.GetAIServiceBreakdown;

public class GetAIServiceBreakdownQueryHandler : IRequestHandler<GetAIServiceBreakdownQuery, AIServiceBreakdownResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private const decimal ExchangeRate = 25400m;

    public GetAIServiceBreakdownQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AIServiceBreakdownResponseDto> Handle(GetAIServiceBreakdownQuery request, CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.Repository<AITokenLog>().FindAsync(log =>
            (!request.StartDate.HasValue || log.CreatedAt >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || log.CreatedAt <= request.EndDate.Value));

        var logList = logs.ToList();

        var costSettings = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();
        var pricingDict = costSettings.ToDictionary(c => c.ModelId, StringComparer.OrdinalIgnoreCase);

        const decimal defaultCostInput = 0.00000059m;
        const decimal defaultCostOutput = 0.00000079m;

        var serviceGroups = logList.GroupBy(l => string.IsNullOrWhiteSpace(l.ServiceType) ? "Unknown" : l.ServiceType);

        var breakdownItems = new List<AIServiceBreakdownItemDto>();
        long totalAllTokens = 0;
        decimal totalAllCostUsd = 0m;

        foreach (var group in serviceGroups)
        {
            var serviceType = group.Key;
            long promptTokens = group.Sum(x => (x.PromptTokens > 0 || x.CompletionTokens > 0) ? (long)x.PromptTokens : 220L);
            long completionTokens = group.Sum(x => (x.PromptTokens > 0 || x.CompletionTokens > 0) ? (long)x.CompletionTokens : 160L);
            decimal serviceCostUsd = 0m;

            foreach (var log in group)
            {
                long p = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.PromptTokens : 220L;
                long c = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.CompletionTokens : 160L;

                var modelId = string.IsNullOrWhiteSpace(log.ModelId) ? "llama-3.3-70b-versatile" : log.ModelId;
                decimal costInput = defaultCostInput;
                decimal costOutput = defaultCostOutput;

                if (pricingDict.TryGetValue(modelId, out var pricing) && pricing.IsActive)
                {
                    costInput = pricing.CostPerInputToken;
                    costOutput = pricing.CostPerOutputToken;
                }

                serviceCostUsd += (p * costInput) + (c * costOutput);
            }

            var groupTokens = promptTokens + completionTokens;
            totalAllTokens += groupTokens;
            totalAllCostUsd += serviceCostUsd;

            breakdownItems.Add(new AIServiceBreakdownItemDto
            {
                ServiceType = serviceType,
                DisplayName = MapServiceDisplayName(serviceType),
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = groupTokens,
                RequestCount = group.Count(),
                EstimatedCostUsd = Math.Round(serviceCostUsd, 6),
                EstimatedCostVnd = Math.Round(serviceCostUsd * ExchangeRate, 0),
                Percentage = 0 // Calculated after
            });
        }

        // Calculate percentages
        if (totalAllTokens > 0)
        {
            foreach (var item in breakdownItems)
            {
                item.Percentage = Math.Round(((double)item.TotalTokens / totalAllTokens) * 100, 2);
            }
        }

        return new AIServiceBreakdownResponseDto
        {
            PeriodStart = request.StartDate,
            PeriodEnd = request.EndDate,
            TotalTokens = totalAllTokens,
            TotalEstimatedCostUsd = Math.Round(totalAllCostUsd, 6),
            TotalEstimatedCostVnd = Math.Round(totalAllCostUsd * ExchangeRate, 0),
            Services = breakdownItems.OrderByDescending(s => s.TotalTokens).ToList()
        };
    }

    private static string MapServiceDisplayName(string serviceType) => serviceType switch
    {
        "ChatRoleplay" => "Thực hành AI (Roleplay)",
        "Scoring" or "ScoringService" => "Chấm điểm tự động (AI Scoring)",
        "RagCourseQnA" or "RagChat" or "CourseChat" => "Trợ lý hỏi đáp khóa học (RAG)",
        "SimulationChat" => "Mô phỏng Chuyên gia (Simulation)",
        _ => serviceType
    };
}
