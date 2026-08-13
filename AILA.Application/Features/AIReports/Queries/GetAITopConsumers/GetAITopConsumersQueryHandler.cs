using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIReports.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIReports.Queries.GetAITopConsumers;

public class GetAITopConsumersQueryHandler : IRequestHandler<GetAITopConsumersQuery, AITopConsumersResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private const decimal ExchangeRate = 25400m;

    public GetAITopConsumersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AITopConsumersResponseDto> Handle(GetAITopConsumersQuery request, CancellationToken cancellationToken)
    {
        var topCount = request.Top <= 0 ? 5 : Math.Min(request.Top, 50);

        // 1. Fetch token logs
        var logs = await _unitOfWork.Repository<AITokenLog>().FindAsync(log =>
            (!request.StartDate.HasValue || log.CreatedAt >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || log.CreatedAt <= request.EndDate.Value));

        var logList = logs.ToList();

        // 2. Fetch pricing
        var costSettings = await _unitOfWork.Repository<AIApiCostSetting>().GetAllAsync();
        var pricingDict = costSettings.ToDictionary(c => c.ModelId, StringComparer.OrdinalIgnoreCase);

        const decimal defaultCostInput = 0.00000059m;
        const decimal defaultCostOutput = 0.00000079m;

        // 3. Calculate Top Users
        var userGroups = logList.GroupBy(l => l.AccountId);
        var userConsumers = new List<TopUserConsumerDto>();

        var accountIds = userGroups.Select(g => g.Key).Distinct().ToList();
        var users = (await _unitOfWork.Repository<User>().FindAsync(u => accountIds.Contains(u.Id)))
            .ToDictionary(u => u.Id);

        foreach (var group in userGroups)
        {
            var accountId = group.Key;
            users.TryGetValue(accountId, out var user);

            long totalTokens = 0;
            decimal totalCostUsd = 0m;

            foreach (var log in group)
            {
                long p = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.PromptTokens : 220L;
                long c = (log.PromptTokens > 0 || log.CompletionTokens > 0) ? (long)log.CompletionTokens : 160L;
                totalTokens += (p + c);

                var modelId = string.IsNullOrWhiteSpace(log.ModelId) ? "llama-3.3-70b-versatile" : log.ModelId;
                decimal costInput = defaultCostInput;
                decimal costOutput = defaultCostOutput;

                if (pricingDict.TryGetValue(modelId, out var pricing) && pricing.IsActive)
                {
                    costInput = pricing.CostPerInputToken;
                    costOutput = pricing.CostPerOutputToken;
                }

                totalCostUsd += (p * costInput) + (c * costOutput);
            }

            userConsumers.Add(new TopUserConsumerDto
            {
                UserId = accountId,
                FullName = user?.FullName ?? "Người dùng",
                Email = user?.Email ?? "N/A",
                Role = user?.Role.ToString() ?? "Learner",
                TotalTokens = totalTokens,
                RequestCount = group.Count(),
                EstimatedCostUsd = Math.Round(totalCostUsd, 6),
                EstimatedCostVnd = Math.Round(totalCostUsd * ExchangeRate, 0)
            });
        }

        var topUsers = userConsumers.OrderByDescending(u => u.TotalTokens).Take(topCount).ToList();

        // 4. Calculate Top Materials
        var attemptIds = logList.Where(l => l.AttemptId.HasValue).Select(l => l.AttemptId!.Value).Distinct().ToList();
        var materialConsumers = new List<TopMaterialConsumerDto>();

        if (attemptIds.Any())
        {
            var practiceAttempts = (await _unitOfWork.Repository<PracticeAttempt>().FindAsync(a => attemptIds.Contains(a.Id))).ToList();
            var simulationAttempts = (await _unitOfWork.Repository<ExpertSimulationAttempt>().FindAsync(a => attemptIds.Contains(a.Id))).ToList();

            var attemptToMaterialMap = new Dictionary<Guid, Guid>();
            foreach (var pa in practiceAttempts) attemptToMaterialMap[pa.Id] = pa.MaterialId;
            foreach (var sa in simulationAttempts) attemptToMaterialMap[sa.Id] = sa.MaterialId;

            var materialIds = attemptToMaterialMap.Values.Distinct().ToList();
            var materials = (await _unitOfWork.Repository<Material>().FindAsync(m => materialIds.Contains(m.Id))).ToDictionary(m => m.Id);

            var materialGroups = logList
                .Where(l => l.AttemptId.HasValue && attemptToMaterialMap.ContainsKey(l.AttemptId.Value))
                .GroupBy(l => attemptToMaterialMap[l.AttemptId!.Value]);

            foreach (var group in materialGroups)
            {
                var materialId = group.Key;
                materials.TryGetValue(materialId, out var mat);

                long totalTokens = 0;
                decimal totalCostUsd = 0m;
                var uniqueAttempts = group.Select(g => g.AttemptId!.Value).Distinct().Count();

                foreach (var log in group)
                {
                    totalTokens += (log.PromptTokens + log.CompletionTokens);

                    var modelId = string.IsNullOrWhiteSpace(log.ModelId) ? "llama-3.3-70b-versatile" : log.ModelId;
                    decimal costInput = defaultCostInput;
                    decimal costOutput = defaultCostOutput;

                    if (pricingDict.TryGetValue(modelId, out var pricing) && pricing.IsActive)
                    {
                        costInput = pricing.CostPerInputToken;
                        costOutput = pricing.CostPerOutputToken;
                    }

                    totalCostUsd += (log.PromptTokens * costInput) + (log.CompletionTokens * costOutput);
                }

                materialConsumers.Add(new TopMaterialConsumerDto
                {
                    MaterialId = materialId,
                    Title = mat?.Title ?? "Bài thực hành AI",
                    CourseTitle = mat?.Module?.Course?.Name ?? "Khóa học AI",
                    TotalAttempts = uniqueAttempts,
                    TotalTokens = totalTokens,
                    EstimatedCostUsd = Math.Round(totalCostUsd, 6),
                    EstimatedCostVnd = Math.Round(totalCostUsd * ExchangeRate, 0)
                });
            }
        }

        var topMaterials = materialConsumers.OrderByDescending(m => m.TotalTokens).Take(topCount).ToList();

        return new AITopConsumersResponseDto
        {
            PeriodStart = request.StartDate,
            PeriodEnd = request.EndDate,
            TopUsers = topUsers,
            TopMaterials = topMaterials
        };
    }
}
