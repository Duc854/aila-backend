using AILA.Application.Common.Interfaces;
using AILA.Application.Features.SubscriptionPlans.Dtos;
using AILA.Application.Features.SubscriptionPlans.Mapping;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan
{
    /// <summary>
    /// UC-91 - Update Subscription Plan.
    /// Handler KHÔNG đụng tới subscription hiện hữu: cấu hình đã bán được bảo toàn bằng
    /// snapshot ở Subscription (INV-03, BR-04, BR-05).
    /// </summary>
    public class UpdateSubscriptionPlanCommandHandler(IUnitOfWork uow)
        : IRequestHandler<UpdateSubscriptionPlanCommand, ResponseDto<AdminSubscriptionPlanDto>>
    {
        public async Task<ResponseDto<AdminSubscriptionPlanDto>> Handle(
            UpdateSubscriptionPlanCommand request,
            CancellationToken ct)
        {
            // AC-91.3, AC-91.6, AC-91.7, AC-91.8 — chặn trước khi chạm entity (AC-91.10).
            var validationError = SubscriptionPlanRules.ValidateCommonFields(
                request.Description,
                request.Price,
                request.AiTokenLimit,
                request.AiPracticeScenarioLimit,
                request.ExpertEvaluationLimit,
                request.DisplayOrder);

            if (validationError is not null)
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    validationError.Value.Code,
                    validationError.Value.Message);

            var plan = await uow.SubscriptionPlans.GetByIdAsync(request.PlanId);

            // AC-91.9: gói không còn tồn tại → báo lỗi để UI đưa admin về danh sách.
            if (plan is null)
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.NotFound,
                    "Không tìm thấy gói đăng ký.");

            try
            {
                // AC-91.2: Update() không nhận Name/TierLevel nên hai trường này bất biến,
                // dù client có gửi kèm. Update() cũng tự gọi UpdateTimestamp().
                plan.Update(
                    request.Description,
                    request.Price,
                    request.AiTokenLimit,
                    request.AiPracticeScenarioLimit,
                    request.ExpertEvaluationLimit,
                    request.DisplayOrder);
            }
            catch (ArgumentException ex)
            {
                return ResponseDto<AdminSubscriptionPlanDto>.FailResult(
                    SubscriptionPlanErrors.ValidationError,
                    ex.Message);
            }

            uow.SubscriptionPlans.Update(plan);

            await uow.SaveChangesAsync(ct);

            return ResponseDto<AdminSubscriptionPlanDto>.SuccessResult(plan.ToAdminDto());
        }
    }
}
