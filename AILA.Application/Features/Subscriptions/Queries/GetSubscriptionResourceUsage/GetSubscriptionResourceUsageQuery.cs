using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Subscriptions.Dtos;
using AILA.Domain.Enums;
using MediatR;
using Shared.Wrappers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.Subscriptions.Queries.GetSubscriptionResourceUsage
{
    public record GetSubscriptionResourceUsageQuery(Guid LearnerId) 
        : IRequest<ResponseDto<SubscriptionResourceUsageDto>>;

    public class GetSubscriptionResourceUsageQueryHandler 
        : IRequestHandler<GetSubscriptionResourceUsageQuery, ResponseDto<SubscriptionResourceUsageDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetSubscriptionResourceUsageQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResponseDto<SubscriptionResourceUsageDto>> Handle(
            GetSubscriptionResourceUsageQuery request, 
            CancellationToken cancellationToken)
        {
            // 1. Lấy thông tin gói đăng ký Active hiện tại (nếu có) - BR-02
            var activeSubscription = await _uow.Subscriptions.GetActiveSubscriptionByLearnerIdAsync(
                request.LearnerId, cancellationToken);

            // 2. Lấy thông tin giới hạn tài nguyên ghi đè riêng của tài khoản (nếu có) - IAccountResourceLimitRepository
            var accountLimit = await _uow.AccountResourceLimits.GetByAccountIdAsync(
                request.LearnerId, cancellationToken);

            // 3. Lấy thông tin sử dụng tài nguyên thực tế của học viên - IAccountResourceUsageRepository
            var usage = await _uow.AccountResourceUsages.GetByAccountIdAsync(
                request.LearnerId, cancellationToken);

            // 4. Lấy chính sách tài nguyên mặc định của nền tảng - IResourceLimitPolicyRepository
            var defaultPolicy = await _uow.ResourceLimitPolicies.GetByAccountTypeAsync(
                ResourceAccountType.Learner, cancellationToken);

            // Xác định định ngạch (Allocated Quota) từng loại tài nguyên
            int aiTokenLimit = accountLimit?.AiTokenLimit 
                ?? activeSubscription?.PlanSnapshot?.AiTokenLimit 
                ?? defaultPolicy?.AiTokenLimit 
                ?? 10000;

            int aiPracticeLimit = accountLimit?.AiPracticeScenarioLimit 
                ?? activeSubscription?.PlanSnapshot?.AiPracticeScenarioLimit 
                ?? defaultPolicy?.AiPracticeScenarioLimit 
                ?? 3;

            int expertEvalLimit = accountLimit?.ExpertEvaluationRequestLimit 
                ?? activeSubscription?.PlanSnapshot?.ExpertEvaluationLimit 
                ?? defaultPolicy?.ExpertEvaluationRequestLimit 
                ?? 1;

            // Số lượng đã dùng
            int aiTokenUsed = usage?.AiTokenUsed ?? 0;
            int aiPracticeUsed = usage?.AiPracticeScenarioUsed ?? 0;
            int expertEvalUsed = usage?.ExpertEvaluationRequestUsed ?? 0;

            var dto = new SubscriptionResourceUsageDto
            {
                HasActiveSubscription = activeSubscription != null,
                SubscriptionId = activeSubscription?.Id,
                SubscriptionPlanId = activeSubscription?.SubscriptionPlanId,
                SubscriptionPlanName = activeSubscription != null
                    ? (activeSubscription.SubscriptionPlan?.Name ?? "Gói Đang Hoạt Động")
                    : "Gói Mặc Định Sàn (Free)",
                TierLevel = activeSubscription?.PlanSnapshot?.TierLevel ?? activeSubscription?.SubscriptionPlan?.TierLevel,
                ActivatedAt = activeSubscription?.ActivatedAt,
                ExpiredAt = activeSubscription?.ExpiredAt,
                RemainingDays = activeSubscription?.GetRemainingDays() ?? 0,
                Resources = new List<ResourceUsageItemDto>
                {
                    new ResourceUsageItemDto
                    {
                        ResourceType = "AiToken",
                        ResourceName = "Hạn mức AI Token",
                        Unit = "Token",
                        AllocatedQuota = aiTokenLimit,
                        UsedQuota = aiTokenUsed,
                        RemainingQuota = Math.Max(0, aiTokenLimit - aiTokenUsed)
                    },
                    new ResourceUsageItemDto
                    {
                        ResourceType = "AiPracticeScenario",
                        ResourceName = "Kịch bản thực hành AI",
                        Unit = "Lượt",
                        AllocatedQuota = aiPracticeLimit,
                        UsedQuota = aiPracticeUsed,
                        RemainingQuota = Math.Max(0, aiPracticeLimit - aiPracticeUsed)
                    },
                    new ResourceUsageItemDto
                    {
                        ResourceType = "ExpertEvaluationRequest",
                        ResourceName = "Yêu cầu đánh giá bài học từ Chuyên gia",
                        Unit = "Lượt",
                        AllocatedQuota = expertEvalLimit,
                        UsedQuota = expertEvalUsed,
                        RemainingQuota = Math.Max(0, expertEvalLimit - expertEvalUsed)
                    }
                }
            };

            return ResponseDto<SubscriptionResourceUsageDto>.SuccessResult(dto);
        }
    }
}
